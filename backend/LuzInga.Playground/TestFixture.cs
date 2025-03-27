using Horus.Modules.Core.Infra;
using Horus.Modules.Core.Infra.Services.RAG;
using Horus.RootBootstrapper;
using LuzInga.Modules.Shared.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class TestFixture : IDisposable
{
    private Action<IServiceCollection, IConfiguration>? _optionsForSystemAction = null;

    public TestFixture()
    {
       
    }

    public void Initialize()
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
        
        if (_optionsForSystemAction != null)
            _optionsForSystemAction(hostBuilder.Services, hostBuilder.Configuration);

        hostBuilder.Services.AddSingleton<IConfiguration>(Configuration);

        // Build the service provider
        ServiceProvider = hostBuilder.Services.BuildServiceProvider();
    }

    public IServiceProvider ServiceProvider { get; private set; }
    public IConfiguration Configuration { get; private set; }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable) disposable.Dispose();
    }

    public void WithOptionsForSystem(Action<RagOptions> action)
    {
        this._optionsForSystemAction = (collection, configuration) =>
        {
            // Remove existing configurations for RagOptions to avoid conflicts
            var existingConfigs = collection
                .Where(d => d.ServiceType == typeof(IConfigureOptions<RagOptions>))
                .ToList();

            foreach (var config in existingConfigs)
            {
                collection.Remove(config);
            }
            
            // Add your custom RagOptions instance
            collection.Configure(action);
        };
    }
}