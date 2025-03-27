using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Domain.Factories;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System;
using Horus.Modules.Core.Infra.Services.PythonServices;
using Microsoft.Extensions.Logging;

namespace Horus.Modules.Core.Infra.Services.RAG;

public class SemanticChunkStrategy : ISemanticChunkStrategy
{
    private readonly IDocumentChunkFactory _chunkFactory;
    private readonly IOptions<RagOptions> _options;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<SemanticChunkStrategy> _logger;
    private readonly ITextProcessingApi _textProcessingApi;

    public SemanticChunkStrategy(
        IDocumentChunkFactory chunkFactory,
        IOptions<RagOptions> options,
        IEmbeddingService embeddingService,
        ILogger<SemanticChunkStrategy> logger,
        ITextProcessingApi textProcessingApi)
    {
        _chunkFactory = chunkFactory;
        _options = options;
        _embeddingService = embeddingService;
        _logger = logger;
        _textProcessingApi = textProcessingApi;
    }

    public IEnumerable<DocumentChunk> SplitChunks(Guid docId, string documentProcessedContent)
    {
        var semanticOptions = _options.Value.ChunkStrategies.SemanticChunk;
        var sentences = SplitIntoSentences(documentProcessedContent).GetAwaiter().GetResult();
        var embeddings = GenerateEmbeddings(sentences).GetAwaiter().GetResult();
        // Estimar o valor de minPts com base nas distâncias
        
        int estimatedMinPts = EstimateMinPts(embeddings);
        var clusters = ClusterSentences(embeddings, semanticOptions, estimatedMinPts);
        return CreateChunksFromClusters(docId, clusters, sentences, embeddings, semanticOptions);
    }

    private async Task<List<string>> SplitIntoSentences(string content)
    {
        var response = await _textProcessingApi.GetSentencesAsync(new GetSentencesRequest()
        {
            Text = content
        });   
        return response.Sentences;
    }

    private async Task<float[][]> GenerateEmbeddings(List<string> sentences)
    {
        // Gerar embeddings para cada sentença
        var results = new List<float[]>();

        for (int i = 0; i < sentences.Count(); i++)
        {
            var result = await _embeddingService.GetEmbeddingAsync(sentences[i], _options.Value.ChunkStrategies.SemanticChunk.EmbeddingModel);
            results.Add(result);
        }

        return results.ToArray();
    }

    private List<List<int>> ClusterSentences(float[][] embeddings, SemanticChunkOptions options, int estimatedMinPts)
    {
        switch (options.ClusteringAlgorithm.ToLower())
        {
            case "hdbscan":
                var hdbscan = new HDBSCAN(
                    embeddings, 
                    minPts: estimatedMinPts,
                    minClusterSize: options.MinClusterSize
                );
                return hdbscan.Cluster();

            case "kmeans":
                var kmeans = new KMeans(
                    embeddings, 
                    k: options.MinClusterSize,
                    maxIterations: 100
                );
                return kmeans.Cluster();

            default:
                throw new NotSupportedException($"Algoritmo {options.ClusteringAlgorithm} não implementado");
        }
    }
    private float CosineSimilarity(float[] vec1, float[] vec2)
    {
        // Calcular similaridade cosseno entre dois vetores
        float dotProduct = 0.0f;
        float norm1 = 0.0f;
        float norm2 = 0.0f;

        for (int i = 0; i < vec1.Length; i++)
        {
            dotProduct += vec1[i] * vec2[i];
            norm1 += vec1[i] * vec1[i];
            norm2 += vec2[i] * vec2[i];
        }

        return dotProduct / (float)(Math.Sqrt(norm1) * Math.Sqrt(norm2));
    }

    private IEnumerable<DocumentChunk> CreateChunksFromClusters(Guid docId,
        List<List<int>> clusters,
        List<string> sentences,
        float[][] embeddings,
        SemanticChunkOptions options)
    {
        var chunks = new List<DocumentChunk>();
        int chunkNumber = 1;

        foreach (var cluster in clusters)
        {
            var chunkText = string.Join(" ", cluster.Select(i => sentences[i]));
            var startOffset = GetStartOffset(sentences, cluster.First());
            var endOffset = GetEndOffset(sentences, cluster.Last());
            var tokenCount = CalculateTokenCount(chunkText);

            var chunk = _chunkFactory.CreatePending(
                docId,
                chunkNumber++,
                chunkText,
                tokenCount,
                startOffset,
                endOffset
            );

            chunk.SetMetadata(new Dictionary<string, object>
            {
                ["clusterSize"] = cluster.Count,
                ["similarityThreshold"] = options.SimilarityThreshold
            });

            chunks.Add(chunk);

            if (options.AllowOverlappingClusters)
            {
                HandleOverlaps(docId,cluster, chunks, sentences, chunkNumber, embeddings, options);
            }
        }

        return chunks;
    }

    private int GetStartOffset(List<string> sentences, int index)
    {
        // Calcular o índice inicial do fragmento no texto original
        return string.Join(" ", sentences.Take(index)).Length;
    }

    private int GetEndOffset(List<string> sentences, int index)
    {
        // Calcular o índice final do fragmento no texto original
        return GetStartOffset(sentences, index + 1);
    }

    private void HandleOverlaps(Guid docId, List<int> currentCluster,
        List<DocumentChunk> chunks,
        List<string> sentences,
        int chunkNumber,
        float[][] embeddings,
        SemanticChunkOptions options)
    {
        if (!options.AllowOverlappingClusters) return;

        // Lógica de expansão de clusters para permitir sobreposição
        var expandedCluster = ExpandCluster(currentCluster, sentences, embeddings, options);
        if (expandedCluster.Any())
        {
            chunks.Add(CreateExpandedChunk(docId, expandedCluster, sentences, chunkNumber, options));
        }
    }

    private List<int> ExpandCluster(List<int> currentCluster,
        List<string> sentences,
        float[][] embeddings,
        SemanticChunkOptions options)
    {
        // Expandir o cluster atual para incluir frases adicionais semelhantes
        var expandedCluster = new List<int>(currentCluster);
        foreach (var sentenceIndex in currentCluster)
        {
            foreach (var otherIndex in Enumerable.Range(0, sentences.Count))
            {
                if (!expandedCluster.Contains(otherIndex) &&
                    CosineSimilarity(embeddings[sentenceIndex], embeddings[otherIndex]) >= options.SimilarityThreshold)
                {
                    expandedCluster.Add(otherIndex);
                }
            }
        }
        return expandedCluster;
    }

    private DocumentChunk CreateExpandedChunk(Guid docId, List<int> expandedCluster,
        List<string> sentences,
        int chunkNumber,
        SemanticChunkOptions options)
    {
        // Criar um novo fragmento a partir do cluster expandido
        var chunkText = string.Join(" ", expandedCluster.Select(i => sentences[i]));
        var startOffset = GetStartOffset(sentences, expandedCluster.First());
        var endOffset = GetEndOffset(sentences, expandedCluster.Last());
        var tokenCount = CalculateTokenCount(chunkText);

        var chunk = _chunkFactory.CreatePending(
            docId,
            chunkNumber,
            chunkText,
            tokenCount,
            startOffset,
            endOffset
        );

        chunk.SetMetadata(new Dictionary<string, object>
        {
            ["clusterSize"] = expandedCluster.Count,
            ["similarityThreshold"] = options.SimilarityThreshold
        });

        return chunk;
    }

    private int CalculateTokenCount(string text)
    {
        // Calcular a contagem de tokens no texto
        return _embeddingService.CalculateToken(text).GetAwaiter().GetResult();
    }
    
    
    public int EstimateMinPts(float[][] embeddings)
    {
        // Forçar o valor de minPts para um valor baixo
        int minPts = Math.Min(2, embeddings.Length / 2); // Forçar minPts para 2 ou 3 no máximo para datasets pequenos

        // Garantir que minPts nunca seja menor que 2 (valor mínimo lógico)
        return Math.Max(minPts, 2);
    }



    private List<double> CalculateDistances(float[][] embeddings)
    {
        var distances = new List<double>();

        for (int i = 0; i < embeddings.Length; i++)
        {
            for (int j = i + 1; j < embeddings.Length; j++)
            {
                double distance = EuclideanDistance(embeddings[i], embeddings[j]);
                distances.Add(distance);
            }
        }

        return distances;
    }

    private double EuclideanDistance(float[] vec1, float[] vec2)
    {
        double sum = 0.0;
        for (int i = 0; i < vec1.Length; i++)
        {
            sum += Math.Pow(vec1[i] - vec2[i], 2);
        }
        return Math.Sqrt(sum);
    }

    private (double mean, double stddev) CalculateMeanAndStdDev(List<double> distances)
    {
        double mean = distances.Average();
        double variance = distances.Select(val => Math.Pow(val - mean, 2)).Average();
        double stddev = Math.Sqrt(variance);
        return (mean, stddev);
    }
    
    public List<double> GetKNearestDistances(float[][] embeddings, int k)
    {
        var distances = new List<double>();

        for (int i = 0; i < embeddings.Length; i++)
        {
            var pointDistances = new List<double>();
            for (int j = 0; j < embeddings.Length; j++)
            {
                if (i != j)
                {
                    double dist = EuclideanDistance(embeddings[i], embeddings[j]);
                    pointDistances.Add(dist);
                }
            }

            // Ordenar e pegar a K-ésima maior distância
            pointDistances.Sort();
            distances.Add(pointDistances[k - 1]);  // K-ésimo vizinho
        }

        return distances;
    }

}

