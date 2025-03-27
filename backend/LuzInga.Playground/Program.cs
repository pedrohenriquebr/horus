using System.Globalization;
using CsvHelper;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Application.Usecases.AddDocument;
using Horus.Modules.Core.Application.Usecases.ClearDocumentsByUserId;
using Horus.Modules.Core.Application.Usecases.GenerateText;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Infra.Services.RAG;
using Horus.RootBootstrapper;
using MediatR;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Horus.Modules.Core.Playground;

public class Program
{
    public static Guid UserId = Guid.Parse("9A311095-3FD7-4129-98BA-33052EC26186");
    public static int NUMBER_OF_DOCUMENTS = 100;

    public static (FileStream, string) GenerateStreamFromString(string s)
    {
        var tempFileName = Path.GetTempFileName();
        using var fileStream = File.Create(tempFileName);
        using var writer = new StreamWriter(fileStream);
        writer.Write(s);
        writer.Flush();
        fileStream.Position = 0;
        writer.Close();
        fileStream.Close();


        return (File.OpenRead(tempFileName),tempFileName);
    }

    public static async Task LoadCollection(HttpClient httpClient, DbContext dbContext)
    {
        do
        {
            Console.WriteLine("Waiting for the server to start...");
            try
            {
                var response = await httpClient.GetAsync("echo");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Server is running...");
                    break;
                }
            }
            catch (Exception)
            {
            }

            Thread.Sleep(1000);
        } while (true);


        Console.WriteLine($"Deleting documents from user {UserId.ToString()}...");
        var _ = await httpClient.DeleteAsync($"documents/by-user-id/{UserId.ToString()}");


        if (!_.IsSuccessStatusCode)
            throw new Exception("Failed to delete documents");


        string collectionPath = "/home/pedrobr/Documents/repos/horus/backend/LuzInga.Playground/collection.tsv";

        CsvHelper.Configuration.CsvConfiguration myConfig = new
            CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "\t",
                HasHeaderRecord = false,
            };

        using var reader = new StreamReader(collectionPath);
        using var csv = new CsvReader(reader, myConfig);
        var records = csv.GetRecords<dynamic>();


        foreach (var record in records.Take(NUMBER_OF_DOCUMENTS).ToList())
        {
            var result = new RouteValueDictionary(record);
            var docId = int.Parse(result.GetValueOrDefault("Field1") as string ??
                                  throw new InvalidOperationException());
            var text = result.GetValueOrDefault("Field2") as string;
            int length = text!.Length;
            int calculatedLength = (int)(length * 0.1);
            Console.WriteLine($"DocId: {docId}, Text: {text.Substring(calculatedLength)}, Length: {length}");

            var (stream,_) = GenerateStreamFromString(text!);
            var form = new MultipartFormDataContent();
            form.Add(new StringContent("__" + docId + "__"), "name");
            form.Add(new StringContent("__" + docId + "__"), "sourceUri");
            form.Add((new StreamContent(stream)), "fileContent", "__" + docId + "__");
            form.Add(new StringContent("semantic_chunk"), "chunkingStrategy");
            form.Add(new StringContent(UserId.ToString()), "userId");
            var httpResponse = await httpClient.PostAsync("documents/add-document", form);
            var responseContent = await httpResponse.Content.ReadAsStringAsync();
            if (!httpResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error when adding document: {docId} \n {responseContent}"); 
            }

            await Task.Delay(Random.Shared.Next(500, 700));

            var isCompleted = false;
            do
            {
                Console.WriteLine($"Waiting for document {docId} to be completed");
                await Task.Delay(1000);
                isCompleted = await dbContext.Set<Document>().CountAsync(d => d.SourceUri == "__" + docId + "__"
                                           && d.StatusId ==(int) DocumentStatusEnum.Completed) > 0;
            } while (!isCompleted);
            
        }
    }

    private static WebApplicationFactoryFixture<ProgramImpl> SetupEnvironment()
    {
        var fixture = new WebApplicationFactoryFixture<ProgramImpl>();
        fixture.WithOptionsForSystem(options =>
        {
            options.SearchLimit = 10;
            options.SearchThresold = 0.65f;
            options.ChunkStrategy = "fixed_chunk";
            options.ChunkStrategies = new ChunkStrategiesOptions
            {
                FixedChunk = new FixedChunkOptions
                {
                    ChunkSize = 25,
                    Overlap = 10
                },
                SemanticChunk = new SemanticChunkOptions()
                {
                    SimilarityThreshold = 0.75f,
                    EmbeddingModel = "all-minilm:l6-v2",
                    AllowOverlappingClusters = false,
                    ClusteringAlgorithm = "HDBSCAN",
                    EmbeddingDimension = 384,
                    MaxClusterSize = 10,
                    MinClusterSize = 2
                }
            };
        });
        

        return fixture;
    }
public static async Task Main(string[] args)
{
    var app = SetupEnvironment();

    using var scope = app.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
    // var (_fileStream, path) = GenerateStreamFromString(Dataset.Humans);
    // _fileStream.Close();
    var path = "";
        
    await mediator.Send(new ClearDocumentsByUserIdCommand()
    {
        UserId = UserId
    });
    
    await ProcessFileUpload("/file:"+path, mediator, dbContext);
    
    Console.WriteLine("Chatbot Console Initialized. Type '/help' for commands.");

    do
    {
        Console.Write("\nYou: ");
        string userPrompt = Console.ReadLine()?.Trim() ?? "";

        switch (userPrompt.ToLower())
        {
            case "/exit":
                Console.WriteLine("Exiting...");
                return;

            case "/help":
                ShowHelp();
                continue;
        }

        if (userPrompt.StartsWith("/file:"))
        {
            await ProcessFileUpload(userPrompt, mediator, dbContext);
            continue;
        }

        var result = await mediator.Send(new GenerateTextQuery
        {
            Prompt = userPrompt,
            UserInfo = new Dictionary<string, string>{{"id", UserId.ToString()}}
        });

        Console.WriteLine($"\nModel: {result.Text}");
        if (result.Sources?.Any() == true)
        {
            Console.WriteLine("\nSources:");
            for (int i = 0; i < result.Sources.Count; i++)
                Console.WriteLine($"[{i + 1}]: {result.Sources[i].Content}");
        }
    }
    while (true);
}

private static void ShowHelp()
{
    Console.WriteLine("Available commands:");
    Console.WriteLine("  /file:<path> - Upload and process a file.");
    Console.WriteLine("  /exit - Exit application.");
    Console.WriteLine("  /help - Show available commands.");
}

private static async Task ProcessFileUpload(string userPrompt, IMediator mediator, DbContext dbContext)
{
    var fullpath = userPrompt.Replace("/file:", "").Trim();

    if (!File.Exists(fullpath))
    {
        Console.WriteLine($"File '{fullpath}' not found.");
        return;
    }

    var fileName = Path.GetFileName(fullpath);

    await using var file = File.Open(fullpath, FileMode.Open);

    Console.WriteLine($"Uploading '{fileName}'...");

    await mediator.Send(new AddDocumentCommand
    {
        ChunkingStrategy = "semantic_chunk",
        FileContent = file,
        SourceUri = "file://" + fullpath,
        Name = fileName,
        UserId = UserId,
    });

    Console.Write("Waiting for document completion...");

    while (await dbContext.Set<Document>().CountAsync(d => d.SourceUri == "file://" + fullpath && d.StatusId == (int)DocumentStatusEnum.Completed) == 0)
    {
        Console.Write(".");
        await Task.Delay(1000);
    }

    Console.WriteLine("\nDocument processing completed successfully.");
}



    public static async Task BenchMarks()
    {
        var app = SetupEnvironment();
        // Get the HttpClient
        var client = app.CreateDefaultClient();
        int totalOfCompletedDocuments = 0;
        var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DbContext>();
        totalOfCompletedDocuments = await context.Set<Document>()
            .CountAsync(d => d.StatusId == (int)DocumentStatusEnum.Completed);

        // if (totalOfCompletedDocuments == 0)
        // {
        await LoadCollection(client, context);
        
        
        do
        {
            totalOfCompletedDocuments = await context.Set<Document>()
                .CountAsync(d => d.StatusId == (int)DocumentStatusEnum.Completed);
            Console.WriteLine($"Total of completed documents: {totalOfCompletedDocuments}");
            await Task.Delay(1000);
        } while (totalOfCompletedDocuments < NUMBER_OF_DOCUMENTS);
        // }

        
        await Task.Delay(2000);
        await Eval(scope.ServiceProvider, client);
        scope.Dispose();
    }

    public static async Task Eval(IServiceProvider serviceProvider, HttpClient client)
    {
        var ragService = serviceProvider.GetRequiredService<IRagService>();
        var options = serviceProvider.GetRequiredService<IOptions<RagOptions>>();
        var dbContext = serviceProvider.GetRequiredService<DbContext>();

        Dictionary<string, List<string>> queryToDocIds = GetQueryToDocIds();

// Definindo o caminho do arquivo de saída
        string outputPath = "/home/pedrobr/Documents/repos/horus/backend/LuzInga.Playground/benchmark_output.txt";

// Variáveis para acumular as métricas
        double totalPrecision = 0;
        double totalRecall = 0;
        double totalF1 = 0;
        int queryCount = 0;

// Usando StreamWriter para escrever no arquivo
        await using StreamWriter writer = new StreamWriter(outputPath);

        Log("Iniciando avaliação das queries...\n\n", writer);
        Log($"RagOptions: {JsonConvert.SerializeObject(options.Value,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            })}", writer);

        var documents = await dbContext.Set<Document>().ToListAsync();

        foreach (var pair in queryToDocIds)
        {
            var query = pair.Key;
            var expected = pair.Value;
            var resultado = (await ragService.SearchSimilarHybridAsync(query, UserId)).ToList();
            var sourceUris = resultado.Select(x => documents.First(d => d.Id == x.DocumentId).SourceUri).ToList();
            var chunkTexts = resultado.Select(x => $"'{x.Content.Substring(Math.Min(x.Content.Length, 10))}...'")
                .ToList();
            var documentIdToSourceUri = resultado
                .DistinctBy(d => d.DocumentId)
                .ToDictionary(x => x.DocumentId,
                    x => documents
                        .First(d => d.Id == x.DocumentId)
                        .SourceUri);

            var totalOfChunksPerDocument = documents.Where(d => d.StatusId == (int)DocumentStatusEnum.Completed)
                .ToDictionary(g => g.SourceUri, g => g.Chunks.Count());

            // Cálculo de precisão e recall
            var relevantRetrieved = expected.Intersect(sourceUris).Count();
            var precision = sourceUris.Count > 0 ? (double)relevantRetrieved / sourceUris.Count : 0;
            var recall = expected.Count > 0 ? (double)relevantRetrieved / expected.Count : 0;

            // Cálculo do F1-score
            double f1 = (precision + recall > 0) ? 2 * (precision * recall) / (precision + recall) : 0;

            // Acumula as métricas
            totalPrecision += precision;
            totalRecall += recall;
            totalF1 += f1;
            queryCount++;

            // Salvando e exibindo os resultados da query
            Log($"Query: {query}", writer);
            Log($"Expected IDs: {string.Join(", ", expected)}", writer);
            Log($"Actual IDs: {string.Join(", ", sourceUris)}", writer);
            Log($"Chunks: {string.Join(";\n", chunkTexts)}", writer);
            Log($"Total chunks retrieved: {resultado.Count}", writer);
            var list = resultado.GroupBy(x =>
                    documentIdToSourceUri[x.DocumentId])
                .Select(g =>
                    $"{g.Key} = {g.Count()} ({(double)g.Count() / totalOfChunksPerDocument[g.Key] * 100.00:F2}%)")
                .ToList();

            Log($"Chunks retrieved per document: {string.Join(" ", list)}", writer);
            Log($"Precision: {precision:F2}", writer);
            Log($"Recall: {recall:F2}", writer);
            Log($"F1-Score: {f1:F2}", writer);
            Log("----------------------------------", writer);
        }

        // Calcula as médias
        double averagePrecision = queryCount > 0 ? totalPrecision / queryCount : 0;
        double averageRecall = queryCount > 0 ? totalRecall / queryCount : 0;
        double averageF1 = queryCount > 0 ? totalF1 / queryCount : 0;

        // Exibe as métricas gerais
        Log("\nMétricas Gerais:", writer);
        Log($"Média da Precisão: {averagePrecision:F2}", writer);
        Log($"Média do Recall: {averageRecall:F2}", writer);
        Log($"Média do F1-Score: {averageF1:F2}", writer);
        Log("Avaliação concluída.", writer);

        try
        {
            // 1) Convert ragOptions to JSON
            var ragOptionsJson = JsonConvert.SerializeObject(options.Value, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None
            });
            
            // 2) Create the body for the new RagExperiment
            var newExperiment = new
            {
                name = "Experiment_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"),
                createdAt = DateTime.UtcNow, // or let DB handle default
                ragOptionsJson = ragOptionsJson,
                averagePrecision = averagePrecision,
                averageRecall = averageRecall,
                averageF1 = averageF1
            };
            
            // 3) Serialize and POST
            var jsonBody = new StringContent(JsonConvert.SerializeObject(newExperiment), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("rag-experiments", jsonBody); // or "api/v1/rag-experiments"
            if (!response.IsSuccessStatusCode)
            {
                var resp = await response.Content.ReadAsStringAsync();
                Log($"Failed to POST experiment. StatusCode: {response.StatusCode}, Body: {resp}", writer);
            }
            else
            {
                Log("Successfully saved experiment results to /api/v1/rag-experiments", writer);
            }
        }
        catch (Exception ex)
        {
            Log($"Exception while saving experiment: {ex.Message}", writer);
        }
    }

    private static Dictionary<string, List<string>> GetQueryToDocIds()
    {
        return new Dictionary<string, List<string>>
        {
            // Simple Queries
            {
                "Manhattan Project",
                new List<string>
                    { "__0__", "__1__", "__2__", "__3__", "__4__", "__5__", "__6__", "__7__", "__8__", "__9__" }
            },
            // Documents mentioning the Manhattan Project, its development, impact, or related events.

            {
                "Restorative Justice",
                new List<string>
                {
                    "__10__", "__11__", "__12__", "__13__", "__14__", "__15__", "__16__", "__17__", "__18__", "__19__"
                }
            },
            // Documents discussing restorative justice principles, practices, or related concepts.

            {
                "Phloem and Xylem",
                new List<string>
                    { "__20__", "__21__", "__22__", "__23__", "__24__", "__25__", "__26__", "__27__", "__28__" }
            },
            // Documents explaining phloem and xylem as plant vascular tissues and their functions.

            { "Chinese Immigration", new List<string> { "__31__", "__33__", "__35__", "__38__" } },
            // Documents about Chinese immigration to the US, including historical context and legislation.

            { "Industrial Workers of the World", new List<string> { "__29__", "__30__", "__36__" } },
            // Documents mentioning the IWW, its membership, and objectives.

            {
                "Medical Tourism in Costa Rica",
                new List<string>
                {
                    "__39__", "__40__", "__41__", "__42__", "__43__", "__44__", "__45__", "__46__", "__47__", "__48__"
                }
            },
            // Documents related to medical tourism in Costa Rica, including services and benefits.

            {
                "Urine Color and Health",
                new List<string>
                {
                    "__49__", "__50__", "__51__", "__52__", "__53__", "__54__", "__55__", "__56__", "__57__", "__58__"
                }
            },
            // Documents discussing how urine color reflects health conditions.

            {
                "Liver Diseases",
                new List<string>
                {
                    "__59__", "__60__", "__61__", "__62__", "__63__", "__64__", "__65__", "__66__", "__67__", "__68__"
                }
            },
            // Documents covering various liver diseases, their causes, and symptoms.

            {
                "Barley Harvest",
                new List<string>
                {
                    "__69__", "__70__", "__71__", "__72__", "__73__", "__74__", "__75__", "__76__", "__77__", "__78__"
                }
            },
            // Documents about barley harvest, its timing, and agricultural significance.

            {
                "Pain under Left Rib Cage",
                new List<string>
                {
                    "__79__", "__80__", "__81__", "__82__", "__83__", "__84__", "__85__", "__86__", "__87__", "__88__"
                }
            },
            // Documents discussing causes of pain under the left rib cage and related anatomy.

            {
                "Wheeler Services",
                new List<string>
                {
                    "__89__", "__90__", "__91__", "__92__", "__93__", "__94__", "__95__", "__96__", "__97__", "__98__"
                }
            },
            // Documents mentioning Wheeler Services or individuals named Wheeler in various contexts.

            { "Antonín Dvořák", new List<string> { "__99__" } },
            // Document providing a biography of Antonín Dvořák.

            // More Complex Queries
            {
                "Atomic Bomb",
                new List<string> { "__0__", "__1__", "__2__", "__3__", "__5__", "__6__", "__7__", "__8__" }
            },
            // Documents specifically mentioning the atomic bomb, its development, or use.

            { "Victim-Offender Mediation", new List<string> { "__10__", "__14__", "__16__" } },
            // Documents focusing on mediation between victims and offenders within restorative justice.

            {
                "Impact of Immigration on Industrial America",
                new List<string> { "__31__", "__32__", "__33__", "__34__", "__35__", "__36__", "__37__", "__38__" }
            },
            // Documents linking immigration (especially Chinese) to the rise of industrial America.

            {
                "Liver Disease Symptoms",
                new List<string> { "__59__", "__60__", "__61__", "__62__", "__63__", "__67__" }
            }
            // Documents detailing symptoms of liver diseases like cirrhosis and cholestasis.
        };
    }

    // Método auxiliar para escrever no console e no arquivo
    private static void Log(string message, StreamWriter writer)
    {
        Console.WriteLine(message);
        writer.WriteLine(message);
    }
}