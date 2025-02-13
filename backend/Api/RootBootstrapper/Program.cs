using System.Text;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Application.Usecases.GenerateText;
using Horus.Modules.Core.Infra;
using Horus.Modules.Core.Infra.Services.RAG;
using Horus.RootBootstrapper;
using LuzInga.Modules.Shared.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var myAllowSpecificOrigins = "development";
builder.Services.AddCors(options =>
{
    options.AddPolicy(myAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            ;
        });
});
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HorusServer", Version = "v1" });
    c.EnableAnnotations();
});

builder.Services.AddScoped<IMemoryProvider, DefaultMemoryProvider>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

var isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") is not null;
var appsettingsFile = new StringBuilder()
    .Append("appsettings.")
    .Append(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .Append(".")
    .Append(isRunningInContainer switch
    {
        true => "container.",
        _ => ""
    })
    .Append("json")
    .ToString();

builder.Configuration
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile(
        appsettingsFile,
        true,
        false
    );

builder
    .AddStartupHandler()
    .AddSharedInfra()
    .AddCore();

builder.Host.UseSerilog(new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger());

builder.Services.AddEndpointsApiExplorer();
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionHandler();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(myAllowSpecificOrigins);
app.UseResponseCaching();
// // app.UseHttpLogging();
// app.UseLoggingEx();
app.UseSession();
app.MapControllers();

// Endpoint principal
app.MapPost("/generate-text", async (
        IMediator mediator,
        [FromBody] GenerateTextQuery request) =>
    {
        var response = await mediator.Send(request);
        return Results.Ok(response);
    })
    .WithName("GenerateText")
    .Produces<GenerateTextQueryResponse>()
    .ProducesProblem(500);


app.Run();