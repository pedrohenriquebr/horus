using Horus.Modules.Shared.Contracts.Configuration;
using Horus.Modules.Shared.Contracts.Services;
using LuzInga.Modules.Shared.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LuzInga.Modules.Shared.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRedis(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        var redisConnectionString = config.GetConnectionString("Redis");
        var serviceProvider = services.BuildServiceProvider();

        var redisConfig = serviceProvider.GetRequiredService<IOptions<RedisConfig>>();

        var keyFactory = serviceProvider.GetRequiredService<IRedisKeyFactory>();

        var options = ConfigurationOptions.Parse(redisConnectionString);
        options.AbortOnConnectFail = false;
        var redisConnection = ConnectionMultiplexer.Connect(options);

        services.AddSingleton<IConnectionMultiplexer>(redisConnection);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = keyFactory.CreateGlobalInstancePrefix();
        });

        services.AddSession();
        services.AddResponseCaching();

        return services;
    }

    public static WebApplicationBuilder AddSharedInfra(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services
            .AddConfiguration(applicationBuilder.Configuration)
            .AddServices()
            .AddRedis(applicationBuilder.Configuration);

        return applicationBuilder;
    }

    //TODO: Migrar para o AutoFac

    // public static WebApplicationBuilder AddSharedInfraAutoFac(this WebApplicationBuilder applicationBuilder)
    // {
    //     applicationBuilder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    //
    //     applicationBuilder.Host.ConfigureContainer<ContainerBuilder>(builder =>
    //     {
    //         builder
    //             .AddConfigurationAutoFac(applicationBuilder.Configuration)
    //             .AddServices()
    //             .AddRedis(applicationBuilder.Configuration);
    //     });
    //
    //     return applicationBuilder;
    // }

    public static IServiceCollection AddConfiguration(
        this IServiceCollection collection,
        IConfiguration config
    )
    {
        return collection
            .Configure<RedisConfig>(opt => config.GetSection(nameof(RedisConfig)).Bind(opt));
    }

    public static IServiceCollection AddServices(this IServiceCollection collection)
    {
        return collection
            .AddSingleton<IRedisKeyFactory, RedisKeyFactory>();
    }
}