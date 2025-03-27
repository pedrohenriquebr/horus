using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Domain.Factories;
using Horus.Modules.Core.Domain.Repositories;
using Horus.Modules.Core.Infra.Context;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horus.Modules.Core.IntegrationTests.Database;

public class DocumentRepositoryTests : IClassFixture<TestFixture>
{
    private readonly IDocumentsRepository _documentRepository;
    private readonly HorusContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDocumentFactory _factory;
    private readonly IDocumentChunkFactory _documentChunkFactory;

    public DocumentRepositoryTests(TestFixture fixture)
    {
        _serviceProvider = fixture.ServiceProvider;
        _documentRepository = _serviceProvider.GetRequiredService<IDocumentsRepository>();
        _context = _serviceProvider.GetRequiredService<HorusContext>();
        _factory = _serviceProvider.GetRequiredService<IDocumentFactory>();
        _documentChunkFactory = _serviceProvider.GetRequiredService<IDocumentChunkFactory>();
        Thread.Sleep(5000); // Rate limit cooldown
    }

    [Fact]
    public async Task InsertAsync_WithValidDocument_CreatesDocument()
    {
        // Arrange
        var document = _factory.CreatePending(
            "test-source-uri",
            "raw content",
            CheckSum("raw content"),
            "fixed_chunk"
        );

        // Act
        await _documentRepository.InsertAsync(document);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _documentRepository.GetAsync(document.Id);
        result.Should().NotBeNull();
        result.RawContent.Should().Be("raw content");

        // Cleanup
        await _documentRepository.DeleteAsync(document.Id);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedDocument_UpdatesDocument()
    {
        // Arrange
        var document = _factory.CreatePending(
            "test-source-uri",
            "initial raw content",
            CheckSum("initial raw content"),
            "fixed_chunk"
        );
        await _documentRepository.InsertAsync(document);
        await _context.SaveChangesAsync();

        // Modify the document
        document.StartProcessing();

        // Act
        await _documentRepository.UpdateAsync(document);
        await _context.SaveChangesAsync();

        // Assert
        var updatedDocument = await _documentRepository.GetAsync(document.Id);
        updatedDocument.Should().NotBeNull();

        // Cleanup
        await _documentRepository.DeleteAsync(document.Id);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingDocument_DeletesDocument()
    {
        // Arrange
        var document = _factory.CreatePending(
            "test-source-uri",
            "raw content",
            CheckSum("raw content"),
            "fixed_chunk"
        );
        await _documentRepository.InsertAsync(document);
        await _context.SaveChangesAsync();

        // Act
        await _documentRepository.DeleteAsync(document.Id);
        await _context.SaveChangesAsync();

        // Assert
        var deletedDocument = await _documentRepository.GetAsync(document.Id);
        deletedDocument.Should().BeNull();
    }
    
    
    [Fact]
public async Task StartProcessing_UpdatesStatusAndLastProcessedAt()
{
    // Arrange
    var document = _factory.CreatePending(
        "test-source-uri",
        "raw content",
        CheckSum("raw content"),
        "fixed_chunk"
    );
    await _documentRepository.InsertAsync(document);
    await _context.SaveChangesAsync();

    // Act
    document.StartProcessing();
    await _documentRepository.UpdateAsync(document);
    await _context.SaveChangesAsync();

    // Assert
    var updatedDocument = await _documentRepository.GetAsync(document.Id);
    updatedDocument.StatusId.Should().Be(2);
    updatedDocument.LastProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

    // Cleanup
    await _documentRepository.DeleteAsync(document.Id);
    await _context.SaveChangesAsync();
}

[Fact]
public async Task FailProcessing_SetsErrorDetailsAndIncrementsRetryCount()
{
    // Arrange
    var document = _factory.CreatePending(
        "test-source-uri",
        "raw content",
        CheckSum("raw content"),
        "fixed_chunk"
    );
    await _documentRepository.InsertAsync(document);
    await _context.SaveChangesAsync();

    document.StartProcessing();

    // Act
    document.FailProcessing("ERR_001", "Simulated failure");
    await _documentRepository.UpdateAsync(document);
    await _context.SaveChangesAsync();

    // Assert
    var updatedDocument = await _documentRepository.GetAsync(document.Id);
    updatedDocument.StatusId.Should().Be(4);
    updatedDocument.ErrorCode.Should().Be("ERR_001");
    updatedDocument.ErrorMessage.Should().Be("Simulated failure");
    updatedDocument.RetryCount.Should().Be(1);

    // Cleanup
    await _documentRepository.DeleteAsync(document.Id);
    await _context.SaveChangesAsync();
}

[Fact]
public async Task CompleteProcessing_SetsStatusToCompleted()
{
    // Arrange
    var document = _factory.CreatePending(
        "test-source-uri",
        "raw content",
        CheckSum("raw content"),
        "fixed_chunk"
    );
    await _documentRepository.InsertAsync(document);
    await _context.SaveChangesAsync();

    document.StartProcessing();
    document.ProcessContent("processed content");
    
    // Act
    document.CompleteProcessing();
    await _documentRepository.UpdateAsync(document);
    await _context.SaveChangesAsync();

    // Assert
    var updatedDocument = await _documentRepository.GetAsync(document.Id);
    updatedDocument.StatusId.Should().Be(3);
    updatedDocument.ProcessedContent.Should().Be("processed content");

    // Cleanup
    await _documentRepository.DeleteAsync(document.Id);
    await _context.SaveChangesAsync();
}

[Fact]
public async Task AddChunk_WithValidDocumentId_PersistsChunk()
{
    // Arrange
    var document = _factory.CreatePending(
        "test-source-uri",
        "raw content",
        CheckSum("raw content"),
        "fixed_chunk"
    );
    await _documentRepository.InsertAsync(document);
    await _context.SaveChangesAsync();

    var chunk = _documentChunkFactory.CreatePending(document.Id,
        1,
        "chunk content",
        2,
        0,
        10
        );
    
    await _context.Set<DocumentChunk>().AddAsync(chunk);
    await _context.SaveChangesAsync();
    
    // Assert
    var retrievedDocument = await _documentRepository.GetAsync(document.Id);
    retrievedDocument.Chunks.Should().ContainSingle(c => c.Id == chunk.Id);

    // Cleanup
    await _documentRepository.DeleteAsync(document.Id);
    await _context.SaveChangesAsync();
}

[Fact]
public async Task AddChunk_AddingToCollection_PersistsChunk()
{
    // Arrange
    var document = _factory.CreatePending(
        "test-source-uri",
        "raw content",
        CheckSum("raw content"),
        "fixed_chunk"
    );

    var chunk = _documentChunkFactory.CreatePending(
        document.Id,
        1,
        "chunk content",
        2,
        0,
        10
    );
    
    document.AddChunk(chunk);
    await _documentRepository.InsertAsync(document);
    await _context.SaveChangesAsync();
    
    // Assert
    var retrievedDocument = await _documentRepository.GetAsync(document.Id);
    retrievedDocument.Chunks.Should().ContainSingle(c => c.Id == chunk.Id);

    // Cleanup
    await _documentRepository.DeleteAsync(document.Id);
    await _context.SaveChangesAsync();
}



[Fact]
public async Task GetByStatus_ReturnsDocumentsWithMatchingStatus()
{
    // Arrange
    var pendingDoc = _factory.CreatePending("uri1", "content1", CheckSum("content1"), "strategy1");
    var processingDoc = _factory.CreatePending("uri2", "content2", CheckSum("content2"), "strategy2");
    processingDoc.StartProcessing();

    await _documentRepository.InsertAsync(pendingDoc);
    await _documentRepository.InsertAsync(processingDoc);
    await _context.SaveChangesAsync();

    // Act
    var pendingResults = await _documentRepository.GetByStatusAsync(1);
    var processingResults = await _documentRepository.GetByStatusAsync(2);

    // Assert
    pendingResults.Should().ContainSingle(d => d.Id == pendingDoc.Id);
    processingResults.Should().ContainSingle(d => d.Id == processingDoc.Id);

    // Cleanup
    await _documentRepository.DeleteAsync(pendingDoc.Id);
    await _documentRepository.DeleteAsync(processingDoc.Id);
    await _context.SaveChangesAsync();
}

    public static string CheckSum(string input)
    {
        // Use input string to calculate MD5 hash
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes); // .NET 5 +
        }
    }
}