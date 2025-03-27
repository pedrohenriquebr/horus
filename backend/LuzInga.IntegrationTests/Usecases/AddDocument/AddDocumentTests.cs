using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Horus.Modules.Core.Application.Usecases.AddDocument;
using Horus.Modules.Core.Infra.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Horus.Modules.Core.IntegrationTests.Usecases.AddDocument;

public class AddDocumentTests : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly IConfiguration _configuration;
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private Dictionary<string, string>? _userInfo;
    private readonly HorusContext _context;

    public AddDocumentTests(TestFixture fixture)
    {
        _serviceProvider = fixture.ServiceProvider;
        _configuration = fixture.Configuration;
        _context = _serviceProvider.GetRequiredService<HorusContext>();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    public async Task InitializeAsync()
    {
        await Task.Delay(5000);
    }

    public Stream GenerateStreamFromString(string s)
    {
        MemoryStream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task AddDocument_Test()
    {
        

        var command = new AddDocumentCommand()
        {
            Name = "Testse" ,
            SourceUri = "https://test.com",
            FileContent = new FileStream("/home/pedrobr/Downloads/credentials.json", FileMode.Open),
        };
        
        await _mediator.Send(command);
        
        Assert.NotNull(_context.Documents);
    }

    public async Task DisposeAsync()
    {
        await _context.Documents
            .Where(d => d.Id != Guid.Empty)
            .ForEachAsync(d =>
            {
                _context.Documents.Remove(d);
            });
        await _context.SaveChangesAsync();
        await Task.CompletedTask;
    }
}