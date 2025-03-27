using Horus.Modules.Core.Infra.Services.RAG;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Horus.Modules.Core.Playground;

public class WebApplicationFactoryFixture<TEntryPoint> : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    private Action<IServiceCollection, IConfiguration>? _optionsForSystemAction = null;

    public string HostUrl { get; set; } = "https://localhost:3000"; // we can use any free port

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseUrls(HostUrl);

        builder.ConfigureServices((context, services) =>
        {
            if (_optionsForSystemAction != null)
                _optionsForSystemAction(services, context.Configuration);
        });
        

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();  // Remove todos os provedores de logging padrão
            // logging.AddFilter("Microsoft", LogLevel.Warning);  // Exibe apenas logs críticos da Microsoft
            // logging.AddFilter("Hangfire", LogLevel.Warning);   // Reduz logs do Hangfire para nível Warning
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        
        var dummyHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());

        var host = builder.Build();
        host.Start();

        return dummyHost;
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.BaseAddress = new Uri(HostUrl + "/api/v1/");
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