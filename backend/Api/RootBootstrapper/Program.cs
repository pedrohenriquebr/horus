using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Hangfire;
using Hangfire.Common;
using Horus.Modules.Core.Application.Common;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Application.Usecases.AddDocument;
using Horus.Modules.Core.Application.Usecases.ClearDocumentsByUserId;
using Horus.Modules.Core.Application.Usecases.GenerateText;
using Horus.Modules.Core.Application.Usecases.GetChatsByUserId;
using Horus.Modules.Core.Application.Usecases.GetUserByEmail;
using Horus.Modules.Core.Application.Usecases.RegisterUser;
using Horus.Modules.Core.Application.Usecases.ValidateCredentials;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Infra;
using Horus.Modules.Core.Infra.Services.RAG;
using Horus.RootBootstrapper;
using LuzInga.Modules.Shared.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Serilog;

var builder = WebApplication.CreateBuilder(args);var myAllowSpecificOrigins = "development";
builder.Services.AddCors(options =>
{
    options.AddPolicy(myAllowSpecificOrigins,
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
            ;
        });
});
// Add services to the container.
// Adicione esta configuração antes de AddControllers()
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HorusServer", Version = "v1" });
    c.EnableAnnotations();
    c.DocumentFilter<CrudGeneratorDocumentFilter>();
});

builder.Services.AddScoped<IMemoryProvider, DefaultMemoryProvider>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient()
    .ConfigureHttpClientDefaults(d =>
    {
        d.ConfigureHttpClient(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(300);
        });
    });

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

builder.Services.AddCrudGenerator(options =>
{
    options.BaseRoute = "/api/v1";
    options.SetterMethodPrefix = "Update";
    options.CrudRoutes.AddRoutes(
        CrudRoute.For<Project>("projects")
            .DisableAntiforgery(),
        CrudRoute.For<ChatSession>("chatsessions")
            .DisableAntiforgery(),
        CrudRoute.For<ChatMessage>("chatmessages")
            .DisableAntiforgery(),
        CrudRoute.For<User>("users")
            .WithCreate(false)
            .WithUpdate(false)
            .DisableAntiforgery(),
        CrudRoute.For<Document>("documents")
            .WithCreate(false)
            .WithUpdate(false)
            .WithRead(true)
            .WithGetAll(true)
            .DisableAntiforgery()
    );
});;


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
// app.UseDefaultFiles();
// app.UseStaticFiles();    


app.UseAuthentication();
app.UseAuthorization();
// Adicione esta linha
app.UseHangfireDashboard();
app.UseCors(myAllowSpecificOrigins);
app.UseResponseCaching();
// // app.UseHttpLogging();
// app.UseLoggingEx();
app.UseSession();
app.MapControllers();
app.MapHangfireDashboard();

// Endpoint principal
app.MapPost("/api/v1/generate-text", async (
        IMediator mediator,
        [FromBody] GenerateTextQuery request) =>
    {
        var response = await mediator.Send(request);
        return Results.Ok(response);
    })
    .WithName("GenerateText")
    .Produces<GenerateTextQueryResponse>()
    .ProducesProblem(500);

app.MapPost("/api/v1/documents/add-document", async (
        IMediator mediator,
        [FromForm] AddDocumentCommandRequest request) => // Use [FromForm]
    {
        await mediator.Send(new AddDocumentCommand()
        {
            FileContent = request.FileContent.OpenReadStream(),
            Name = request.Name,
            ProjectId = request.ProjectId,
            SourceUri = request.SourceUri,
            ChunkingStrategy = request.ChunkingStrategy,
            UserId = request.UserId,
        });
        return Results.Ok();
    })
    .DisableAntiforgery()
    .WithName("AddDocument")
    .Produces(200)
    .ProducesProblem(500);

app.MapPost("/api/v1/users/register", async (
        IMediator mediator,
        [FromBody] RegisterUserCommand request) => // Use [FromForm]
    {
        await mediator.Send(request);
        return Results.Ok();
    })
    .DisableAntiforgery()
    .WithName("RegisterUser")
    .Produces(200)
    .ProducesProblem(500);


app.MapPost("/api/v1/auth/validate", async (
        IMediator mediator,
        [FromBody] ValidateCredentialsQuery request) => // Use [FromForm]
    {
        var response = await mediator.Send(request);
        if(response is null)
            return Results.NotFound();
        return Results.Ok(response);
    })
    .DisableAntiforgery()
    .WithName("ValidateCredentials")
    .Produces(200)
    .ProducesProblem(500);


app.MapGet("/api/v1/users/get-by-email/{email}", async (
        IMediator mediator,
        string email) => 
    {
        var response = await mediator.Send(new GetUserByEmailQuery()
        {
            Email = email
        });
        
        if(response is null)
            return Results.NotFound();
        return Results.Ok(response);
    })
    .DisableAntiforgery()
    .WithName("GetUserByEmail")
    .Produces(200)
    .ProducesProblem(500);


app.MapGet("/api/v1/chatsessions/get-by-user-id/{userId}", async (
        IMediator mediator,
        string userId,
        [FromQuery] int offset = 1,
        [FromQuery] int pageSize = 10) => 
    {
        var response = await mediator.Send(new GetChatSessionsByUserIdQuery()
        {
            UserId = Guid.Parse(userId),
            Offset = offset,
            PageSize = pageSize,
        });
        
        if(response is null)
            return Results.NotFound();
        return Results.Ok(response);
    })
    .DisableAntiforgery()
    .WithName("GetChatSessionsByUserId")
    .Produces(200)
    .ProducesProblem(500);

app.MapGet("/api/v1/echo", () =>
{
    return Results.Ok("Hello World!");
})
    .DisableAntiforgery()
    .WithName("Echo")
    .Produces(200)
    .ProducesProblem(500);



app.MapDelete("api/v1/documents/by-user-id/{userId}", async (IMediator mediator, string userId) =>
    {

        var response = new ClearDocumentsByUserIdCommand()
        {
            UserId = Guid.Parse(userId)
        };
        
        await mediator.Send(response);
        return Results.Ok();
    })
    .DisableAntiforgery()
    .WithName("ClearDocumentsByUserId")
    .Produces(200)
    .ProducesProblem(500);

// app.MapFallbackToFile("/chat/{*slug}", "index.html");
app.MapFallbackToFile("/dashboard/index.html");
app.UseCrudGenerator();

app.MapHub<ChatSessionHub>("/chatsessions");
app.MapRagExperimentEndpoints();
app.Run();

public partial class Program { }