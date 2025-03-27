using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.PostgreSql.Factories;
using Horus.Modules.Core.Application;
using Horus.Modules.Core.Application.Common;
using Horus.Modules.Core.Application.Common.Behaviors;
using Horus.Modules.Core.Application.Common.Decorators;
using Horus.Modules.Core.Application.Common.Helpers;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Application.Usecases.RegisterUser;
using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Domain.Factories;
using Horus.Modules.Core.Domain.Repositories;
using Horus.Modules.Core.Infra.Context;
using Horus.Modules.Core.Infra.Context.Interceptors;
using Horus.Modules.Core.Infra.Services;
using Horus.Modules.Core.Infra.Services.Embedding;
using Horus.Modules.Core.Infra.Services.LLMProvider;
using Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi;
using Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;
using Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;
using Horus.Modules.Core.Infra.Services.PythonServices;
using Horus.Modules.Core.Infra.Services.RAG;
using Horus.Modules.Core.Infra.Services.RateLimiter;
using Horus.Modules.Core.Infra.Services.Repositories;
using Horus.Modules.Core.Infra.Services.Tools.FileSystem;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Npgsql;
using Refit;
using JsonConverter = Newtonsoft.Json.JsonConverter;

namespace Horus.Modules.Core.Infra;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddInfra(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddDbContext(builder.Configuration)
            .AddFactories()
            .AddRepositories()
            .AddFileSystemServices(builder.Configuration)
            .AddLlmServices(builder.Configuration)
            .AddTextProcessingApi(builder.Configuration)
            .AddSignalRHub()
            .AddMediator()
            .AddOptionsForServices(builder.Configuration)
            .AddServices()
            .AddHangfire(c =>
            {
                var serializerOptions = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.All, 
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                    ContractResolver = new PrivateResolver(),
                    Converters = new List<JsonConverter>()
                    {
                        new NpgsqlTsVectorConverter()
                    }
                };
               


                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                c.UseSerializerSettings(serializerOptions);
                
                c.UsePostgreSqlStorage(c =>
                {
                    var factory = new NpgsqlConnectionFactory(connectionString, new ()
                    {
                        UseSlidingInvisibilityTimeout = true,
                        InvisibilityTimeout = TimeSpan.FromHours(12),
                        DistributedLockTimeout = TimeSpan.FromHours(12),
                    });
                    c.UseConnectionFactory(factory);
                    // c.UseNpgsqlConnection(connectionString);
                    // c.InvisibilityTimeout = TimeSpan.FromHours(12); // Ajuste conforme necessário
                    // c.DistributedLockTimeout = TimeSpan.FromHours(12); // Ajuste conforme necessário
                    // c.UseSlidingInvisibilityTimeout = true;
                });
            })
            .AddHangfireServer();

        return builder;
    }


    public static IServiceCollection AddSignalRHub(this IServiceCollection collection)
    {
        collection
            .AddSignalR();

        return collection;
    }

    public static IServiceCollection AddFactories(this IServiceCollection collection)
    {
        return collection
            .AddScoped<IDocumentChunkFactory, DocumentChunkFactory>()
            .AddScoped<IDocumentFactory, DocumentFactory>();
    }


    public static IServiceCollection AddOptionsForServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SecurityOptions>(configuration.GetSection("SecurityOptions"));
        services.Configure<SupabaseOptions>(configuration.GetSection("Supabase"));
        services.Configure<RagOptions>(configuration.GetSection("RagOptions"));
        
        return services;
    }
    
public static IServiceCollection AddMediator(this IServiceCollection collection)
{
    var assembly = AppDomain.CurrentDomain.Load("Horus.Modules.Core.Application");
    var assembly2 = AppDomain.CurrentDomain.Load("Horus.Modules.Core.Infra");

    // Create a dictionary to track handlers by their service type
    var handlerTracker = new Dictionary<Type, List<ServiceDescriptor>>();

    collection
        .AddMediatR(c =>
        {
            c.RegisterServicesFromAssemblies(assembly, assembly2);
        })
        .AddValidatorsFromAssembly(assembly)
        .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))
        .AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        ;
    

    // Remove registros duplicados prévios (se existirem)
    var existingDecorators = collection
        .Where(d => d.ServiceType == typeof(INotificationHandler<>) 
                    && d.ImplementationType == typeof(UnitOfWorkDecorator<>))
        .ToList();

    foreach (var decorator in existingDecorators)
    {
        collection.Remove(decorator);
    }
    
    collection.Decorate(typeof(INotificationHandler<>), typeof(UnitOfWorkDecorator<>));

    // Enhanced debugging logic
    var handlers = collection
        .Where(x => x.ServiceType.IsAssignableTo(typeof(INotificationHandler<>)))
        .ToList();

    foreach (var handler in handlers)
    {
        var serviceType = handler.ServiceType;
        if (!handlerTracker.ContainsKey(serviceType))
        {
            handlerTracker[serviceType] = new List<ServiceDescriptor>();
        }
        handlerTracker[serviceType].Add(handler);

        Console.WriteLine($"Handler Registration:");
        Console.WriteLine($"- Service Type: {serviceType.FullName}");
        Console.WriteLine($"- Implementation Type: {handler.ImplementationType?.FullName}");
        Console.WriteLine($"- Lifetime: {handler.Lifetime}");
    }

    // Check for duplicates
    foreach (var (serviceType, registrations) in handlerTracker)
    {
        if (registrations.Count > 1)
        {
            Console.WriteLine($"\nWARNING: Duplicate handlers found for {serviceType.FullName}:");
            foreach (var reg in registrations)
            {
                Console.WriteLine($"- {reg.ImplementationType?.FullName}");
            }
        }
    }

    return collection;
}
    

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // Storage

        services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
        services.AddScoped<IFixedChunkStrategy, FixedChunkStrategy>();
        services.AddScoped<ISemanticChunkStrategy, SemanticChunkStrategy>();

        services.AddScoped<IDocumentsRepository, DocumentsRepository>();
        services.AddScoped<IRagService, PostgresRagService>();

        services.AddScoped<IMemoryProvider, DefaultMemoryProvider>();
        services.AddMemoryCache();
        
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        return services;
    }

    public static IServiceCollection AddFileSystemServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IContentTypeProvider, FileExtensionContentTypeProvider>();
        services.AddSingleton<IFileSystemProvider, LocalFileSystemProvider>();

        return services;
    }


    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection AddDbContext(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        var connectionString = config.GetConnectionString("DefaultConnection");
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton<UpdateAuditableEntities>();
        // Adiciona o DbContext usando PostgreSQL (não o Supabase)
        services.AddDbContext<HorusContext>((sp, options) =>
        {
            var auditableInterceptor = sp.GetRequiredService<UpdateAuditableEntities>();
            options
                .UseLazyLoadingProxies()
                .UseNpgsql(dataSource, npgsqlOptions =>
                {
                    npgsqlOptions.UseVector();
                    npgsqlOptions.CommandTimeout((int)TimeSpan.FromMinutes(5).TotalSeconds);
                })
                .EnableSensitiveDataLogging()
                .AddInterceptors(auditableInterceptor);
        });
            
        services.AddScoped<IHorusContext>(sp => sp.GetRequiredService<HorusContext>());
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<HorusContext>());

        services.AddScoped<IUnitOfWork>(
            sp =>
                sp.GetRequiredService<HorusContext>()
                    .WithMediator(sp.GetRequiredService<IMediator>())
        );

        return services;
    }

    private static IServiceCollection AddTextProcessingApi(this IServiceCollection services, IConfiguration configuration)
    {

        var options = configuration.GetSection(nameof(TextProcessingApi)).Get<TextProcessingApi>()!;
        
        services.AddRefitClient<ITextProcessingApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Converters =
                    {
                        new JsonStringEnumConverter()
                    }
                })
            })
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(options.Host)) // Ajuste a URL conforme a configuração da sua API Python
            .AddHttpMessageHandler<ApiLoggingHandler>(); // Se necessário, adicione um manipulador de mensagens para logs

        return services;
    }

    private static IServiceCollection AddLlmServices(this IServiceCollection services, IConfiguration configuration)
    {
        #region Gemini

        var geminiConfig = configuration.GetSection("Gemini").Get<GeminiConfig>();

        services.Configure<GeminiConfig>(configuration.GetSection("Gemini"));

        services.AddTransient<ApiLoggingHandler>();

        services.AddRefitClient<IGeminiApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Converters =
                    {
                        new JsonStringEnumConverter(),
                        new ObjectToInferredTypesConverter()
                    }
                })
            })
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/"))
            .AddHttpMessageHandler<ApiLoggingHandler>();

        services.AddSingleton<IRateLimiter>(provider =>
            new TokenBucketRateLimiter(
                geminiConfig.TokensPerSecond,
                geminiConfig.Burst
            ));

        #endregion

        #region Ollama

        var ollamaConfig = configuration.GetSection("Ollama").Get<OllamaConfig>();
        services.Configure<OllamaConfig>(configuration.GetSection("Ollama"));
        services.AddRefitClient<IOllamaApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Converters =
                    {
                        new JsonStringEnumConverter(),
                        new ObjectToInferredTypesConverter()
                    }
                })
            })
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(ollamaConfig.Url))
            .AddHttpMessageHandler<ApiLoggingHandler>();

        #endregion

        // Alterado para Singleton para evitar problemas de lifetime
        services.AddScoped<SearchWebTool>();
        services.AddScoped<IWebPageCacheProvider, RedisWebPageCacheProvider>();
        services.AddScoped<IToolMediator>(sp =>
        {
            var instance = new ToolMediator(sp.GetRequiredService<ILogger<ToolMediator>>());
            // Resolvendo a dependência no mesmo lifetime
            instance.RegisterTool("search", sp.CreateScope().ServiceProvider.GetRequiredService<SearchWebTool>());
            return instance;
        });

        // Alterado para Singleton para manter consistência
        services.AddScoped<GeminiProvider>(d => new GeminiProvider(
                d.GetRequiredService<IRateLimiter>(),
                d.GetRequiredService<ILogger<GeminiProvider>>(),
                d.GetRequiredService<IOptions<GeminiConfig>>(),
                d.GetRequiredService<IGeminiApi>()
            )
        );
        
        services.AddScoped<ITextSummarizer, GeminiProvider>();
        services.AddScoped<OllamaProvider>();
        services.AddScoped<ILlmProvider, DefaultLlmProviderFallback>();

        return services;
    }

    public static WebApplicationBuilder AddCore(this WebApplicationBuilder appBuilder)
    {
       
        
        appBuilder
            .AddInfra()
            .AddDomain();

        appBuilder.Services.AddApplication(appBuilder.Configuration);

        return appBuilder;
    }
}

public class GeminiConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public double TokensPerSecond { get; set; }
    public int Burst { get; set; }
}