using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Horus.Modules.Core.Application;
using Horus.Modules.Core.Application.Common.Behaviors;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Infra.Context;
using Horus.Modules.Core.Infra.Services;
using Horus.Modules.Core.Infra.Services.Embedding;
using Horus.Modules.Core.Infra.Services.LLMProvider;
using Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi;
using Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;
using Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;
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
using Npgsql;
using Refit;

namespace Horus.Modules.Core.Infra;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddInfra(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddDbContext(builder.Configuration)
            .AddRepositories()
            .AddFileSystemServices(builder.Configuration)
            .AddLlmServices(builder.Configuration)
            .AddSignalRHub()
            .AddMediator()
            .AddServices(builder.Configuration);

        return builder;
    }


    public static IServiceCollection AddSignalRHub(this IServiceCollection collection)
    {
        collection
            .AddSignalR();

        return collection;
    }

    public static IServiceCollection AddMediator(this IServiceCollection collection)
    {
        var assembly = AppDomain.CurrentDomain.Load("Horus.Modules.Core.Application");
        var assembly2 = AppDomain.CurrentDomain.Load("Horus.Modules.Core.Infra");
        collection
            .AddMediatR(c =>
                c.RegisterServicesFromAssemblies(assembly, assembly2)
            )
            .AddValidatorsFromAssembly(assembly)
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        return collection;
    }


    public static IServiceCollection AddServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Storage
        services.Configure<SupabaseOptions>(configuration.GetSection("Supabase"));

        services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();


        services.AddScoped<IDocumentsRepository, DocumentsRepository>();
        services.AddScoped<IRagService, PostgresRagService>();

        services.AddScoped<IMemoryProvider, DefaultMemoryProvider>();
        services.AddMemoryCache();
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
        // Adiciona o DbContext usando PostgreSQL (não o Supabase)
        services.AddDbContext<HorusContext>(options =>
            options.UseNpgsql(dataSource, npgsqlOptions => npgsqlOptions.UseVector()));

        services.AddScoped<IHorusContext>(sp => sp.GetRequiredService<HorusContext>());

        services.AddScoped<IUnitOfWork>(
            sp =>
                sp.GetRequiredService<HorusContext>()
                    .WithMediator(sp.GetRequiredService<IMediator>())
        );

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
        services.AddSingleton<SearchWebTool>();
        services.AddSingleton<IToolMediator>(sp =>
        {
            var instance = new ToolMediator(sp.GetRequiredService<ILogger<ToolMediator>>());
            // Resolvendo a dependência no mesmo lifetime
            instance.RegisterTool("search", sp.GetRequiredService<SearchWebTool>());
            return instance;
        });

        // Alterado para Singleton para manter consistência
        services.AddSingleton<GeminiProvider>();
        services.AddSingleton<OllamaProvider>();
        services.AddSingleton<ILlmProvider, DefaultLlmProviderFallback>();

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