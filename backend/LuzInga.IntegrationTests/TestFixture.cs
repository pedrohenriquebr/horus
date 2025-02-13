using System;
using Horus.Modules.Core.Infra;
using Horus.RootBootstrapper;
using LuzInga.Modules.Shared.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Horus.Modules.Core.IntegrationTests;

public class TestFixture : IDisposable
{
    public TestFixture()
    {
        // Setup Configuration
        var hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.test.json", true)
            .AddEnvironmentVariables();

        Configuration = hostBuilder.Configuration;


        // Add core services using the same DI setup as the main application
        hostBuilder
            .AddStartupHandler()
            .AddSharedInfra()
            .AddCore();

        hostBuilder.Services.AddSingleton<IConfiguration>(Configuration);

        // Build the service provider
        ServiceProvider = hostBuilder.Services.BuildServiceProvider();
    }

    public IServiceProvider ServiceProvider { get; }
    public IConfiguration Configuration { get; }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable) disposable.Dispose();
    }
}