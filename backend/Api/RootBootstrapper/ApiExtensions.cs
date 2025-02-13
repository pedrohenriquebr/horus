using Horus.Modules.Shared.Contracts.SharedKernel.Exceptions;
using Horus.RootBootstrapper.Middlewares;
using Microsoft.AspNetCore.Diagnostics;

namespace Horus.RootBootstrapper;

public static class ApiExtensions

{
    public static IApplicationBuilder UseResponseCachingExtended(this WebApplication app)
    {
        return app.UseMiddleware<CachingMiddleware>();
    }

    public static IApplicationBuilder UseGlobalExceptionHandler(this WebApplication app)
    {
        return app.UseExceptionHandler(a => a.Run(async context =>
        {
            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();


            var originalException = exceptionHandlerPathFeature?.Error;

            if (originalException is null)
                return;

            if (originalException is GlobalApplicationException globalException)
            {
                context.Response.StatusCode = (int)globalException.Type;
                await context.Response.WriteAsJsonAsync(new
                {
                    Type = globalException.Type.ToString(),
                    globalException.Message,
                    ErrorCode = globalException.Code,
                    globalException.Errors
                });
                logger.LogError(originalException, "A global application exception occurred"); // Log the exception
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    Type = ApplicationExceptionType.Application.ToString(),
                    originalException.Message,
                    Details = originalException.StackTrace
                });
                logger.LogError(originalException, "An unhandled exception occurred"); // Log the exception
            }
        }));
    }

    public static WebApplicationBuilder AddStartupHandler(this WebApplicationBuilder app)
    {
        var configuration = app.Configuration;
        Console.WriteLine("Environment variables:");
        foreach (string envVar in Environment.GetEnvironmentVariables().Keys)
            Console.WriteLine($"\t{envVar}: {Environment.GetEnvironmentVariable(envVar)}");

        Console.WriteLine("\nAppSettings variables:");
        foreach (var appSetting in configuration.AsEnumerable())
            Console.WriteLine($"\t{appSetting.Key}: {appSetting.Value}");

        return app;
    }

    public static IServiceCollection AddStartupHandler(this IServiceCollection app, IConfiguration configuration)
    {
        Console.WriteLine("Environment variables:");
        foreach (string envVar in Environment.GetEnvironmentVariables().Keys)
            Console.WriteLine($"\t{envVar}: {Environment.GetEnvironmentVariable(envVar)}");

        Console.WriteLine("\nAppSettings variables:");
        foreach (var appSetting in configuration.AsEnumerable())
            Console.WriteLine($"\t{appSetting.Key}: {appSetting.Value}");

        return app;
    }
}