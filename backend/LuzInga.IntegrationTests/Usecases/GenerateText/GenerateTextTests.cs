using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Application.Usecases.GenerateText;
using Horus.Modules.Core.Domain.LLM;
using Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Horus.Modules.Core.IntegrationTests.Usecases.GenerateText;

public class GenerateTextTests : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly IConfiguration _configuration;
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private Dictionary<string, string>? _userInfo;

    public GenerateTextTests(TestFixture fixture)
    {
        _serviceProvider = fixture.ServiceProvider;
        _configuration = fixture.Configuration;
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    public async Task InitializeAsync()
    {
        await Task.Delay(5000);
    }

    public async Task DisposeAsync()
    {
        var chatHistory = _serviceProvider.GetRequiredService<IChatHistoryProvider>();
        var memoryProvider = _serviceProvider.GetRequiredService<IMemoryProvider>();
        var userInfo = _userInfo;
        if (userInfo != null) await chatHistory.ClearHistoryAsync(userInfo);
        if (_userInfo != null) await memoryProvider.ClearMemoriesAsync(_userInfo);
    }


    [Fact]
    public async Task Handle_ValidPrompt_ShouldReturnGeneratedText()
    {
        // Arrange
        var prompt = "Test prompt";
        _userInfo = new Dictionary<string, string>
        {
            ["id"] = "123456",
            ["first_name"] = "Test User",
            ["username"] = "testuser",
            ["language_code"] = "en"
        };

        // Act
        var result = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = prompt,
            UserInfo = _userInfo
        });

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithCreatorId_ShouldGenerateSpecialResponse()
    {
        // Arrange
        var prompt = "Test prompt";
        _userInfo = new Dictionary<string, string>
        {
            ["id"] = "247554895", // Creator ID
            ["first_name"] = "Pedro",
            ["username"] = "pedrobraga"
        };

        // Act
        var result = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = prompt,
            UserInfo = _userInfo
        });

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithLongPrompt_ShouldProcessSuccessfully()
    {
        // Arrange
        var prompt = new string('x', 1000); // Long prompt
        _userInfo = new Dictionary<string, string> { ["id"] = "123456" };

        // Act
        var result = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = prompt,
            UserInfo = _userInfo
        });

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithEmptyUserInfo_ShouldNotStillWork()
    {
        // Arrange
        var prompt = "Test prompt";
        _userInfo = new Dictionary<string, string>();

        // Act
        await Assert.ThrowsAnyAsync<Exception>(() => _mediator.Send(new GenerateTextQuery
        {
            Prompt = prompt,
            UserInfo = _userInfo
        }));
    }

    [Fact]
    public async Task Handle_RetrievingChatHistory_ShouldReturnCorrectResponse()
    {
        // Arrange
        _userInfo = new Dictionary<string, string> { ["id"] = "123456" };

        // Send messages telling about a fairy tale
        await _mediator.Send(new GenerateTextQuery
        {
            Prompt = "Era uma vez uma bela princesa chamada Rosa.",
            UserInfo = _userInfo
        });
        await _mediator.Send(new GenerateTextQuery
        {
            Prompt = "Ela morava em um castelo com sua mae, a Rainha.",
            UserInfo = _userInfo
        });
        await _mediator.Send(new GenerateTextQuery
        {
            Prompt = "Um dia, uma bruxa malvada lan ou um feitiço o sobre o castelo.",
            UserInfo = _userInfo
        });
        await _mediator.Send(new GenerateTextQuery
        {
            Prompt = "Rosa era a unica que podia quebrar o feitiço.",
            UserInfo = _userInfo
        });

        // Ask about some character
        var result = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = "Quem era a bela princesa? Responda com o nome dela",
            UserInfo = _userInfo
        });

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Contain("Rosa");

        // Cleanup
    }

    [Fact]
    public async Task Handle_ShouldStoreMemory_WhenTaggedInResponse()
    {
        // Arrange
        var prompt = "Minha cor favorita é azul";
        _userInfo = new Dictionary<string, string>
        {
            ["id"] = "testUser",
            ["first_name"] = "Test",
            ["username"] = "testuser"
        };

        // Act
        var result = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = prompt,
            UserInfo = _userInfo
        });

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().NotContain("<store_memory>");

        var memoryProvider = _serviceProvider.GetRequiredService<IMemoryProvider>();
        var memories = await memoryProvider.GetMemoriesAsync(_userInfo);
        memories.Should().Contain(m => m.Content.Contains("azul"));
    }

    [Fact]
    public async Task Handle_ShouldStoreMultipleMemories()
    {
        // Arrange
        var prompt = "I have two cats: <store_memory>Whiskers</store_memory> and <store_memory>Snowball</store_memory>";
        _userInfo = new Dictionary<string, string>
        {
            ["id"] = "multiMemoryUser",
            ["first_name"] = "Multi",
            ["username"] = "multiuser"
        };

        // Act
        var response = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = prompt,
            UserInfo = _userInfo
        });

        // Assert
        response.Should().NotBeNull();
        response.Text.Should().NotContain("<store_memory>");

        var memoryProvider = _serviceProvider.GetRequiredService<IMemoryProvider>();
        var memories = await memoryProvider.GetMemoriesAsync(_userInfo);

        memories.Should()
            .Contain(m => m.Content.Contains("Whiskers"))
            .And.Contain(m => m.Content.Contains("Snowball"));
    }

    [Fact]
    public async Task Handle_ShouldIsolateMemoriesBetweenUsers()
    {
        // Arrange
        var userA = new Dictionary<string, string> { ["id"] = "userA", ["username"] = "a" };
        var userB = new Dictionary<string, string> { ["id"] = "userB", ["username"] = "b" };

        var prompt = "i like icecream, remember that";

        // Act
        await _mediator.Send(new GenerateTextQuery
        {
            Prompt = prompt,
            UserInfo = userA
        });

        // Assert
        var memoryProvider = _serviceProvider.GetRequiredService<IMemoryProvider>();

        var memoriesA = await memoryProvider.GetMemoriesAsync(
            userA
        );

        var memoriesB = await memoryProvider.GetMemoriesAsync(
            userB
        );

        // memoriesA.Should().ContainSingle();
        memoriesB.Should().BeEmpty();

        //cleanup
        var chatHistoryProvider = _serviceProvider.GetRequiredService<IChatHistoryProvider>();
        await chatHistoryProvider.ClearHistoryAsync(userA);
        await memoryProvider.ClearMemoriesAsync(userA);
        await chatHistoryProvider.ClearHistoryAsync(userB);
        await memoryProvider.ClearMemoriesAsync(userB);
    }

    [Theory]
    [InlineData("My favorite color is blue", "en-US", "azul")]
    [InlineData("Eu moro no Rio de Janeiro", "pt-BR", "Rio de Janeiro")]
    [InlineData("mi bebida favorita es el café negro", "es", "café")]
    public async Task Handle_ShouldStoreMultilingualMemories(string prompt, string languageCode, string expectedMemory)
    {
        var memoryProvider = _serviceProvider.GetRequiredService<IMemoryProvider>();
        // Arrange
        _userInfo = new Dictionary<string, string>
        {
            ["id"] = "multilingualUser",
            ["language_code"] = languageCode
        };
        await memoryProvider.ClearMemoriesAsync(_userInfo);

        // Act
        var response = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = prompt,
            UserInfo = _userInfo
        });

        // Assert

        var memories = await memoryProvider.GetMemoriesAsync(
            _userInfo
        );


        memories.Should().Contain(m => m.Content.ToLower().Contains(expectedMemory.ToLower()));
        response.Text.Should().NotContain("<store_memory>");
    }

    [Fact]
    public async Task Handle_ShouldStoreSpecialCharacterMemories()
    {
        // Arrange
        var specialMemory = "ABC-123@#!";
        var prompt = $"Minha senha é \"{specialMemory}\" lembre dela";
        _userInfo = new Dictionary<string, string> { ["id"] = "specialCharUser" };

        // Act
        var response = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = prompt,
            UserInfo = _userInfo
        });

        // Assert
        var memoryProvider = _serviceProvider.GetRequiredService<IMemoryProvider>();
        var memories = await memoryProvider.GetMemoriesAsync(
            _userInfo
        );

        memories.Should().ContainSingle().Which.Content.Should().Contain(specialMemory);
    }

    [Fact]
    public async Task Handle_ShouldPrioritizeUserMemoriesOverGeneralKnowledge()
    {
        // Arrange
        _userInfo = new Dictionary<string, string> { ["id"] = "memoryPriorityUser" };

        // Store custom memory
        await _mediator.Send(new GenerateTextQuery
        {
            Prompt = "Eu prefiro interface no modo escuro",
            UserInfo = _userInfo
        });

        // Act
        var response = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = "Quais são minhas preferências de interface?",
            UserInfo = _userInfo
        });

        // Assert
        response.Text.Should().Contain("modo escuro")
            .And.NotContain("modo claro");
    }


    [Fact]
    public async Task Handle_ShouldUseSecondaryServiceForFallback()
    {
        // Arrange
        _userInfo = new Dictionary<string, string> { ["id"] = "fallbackUser" };
        var prompt = "Test fallback prompt";

        // Force primary service (Gemini) to fail by disconnecting internet or using invalid API key
        var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
        var geminiSection = configuration.GetSection("Gemini");
        geminiSection["ApiKey"] = "invalid_key";

        // Act
        var result = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = prompt,
            UserInfo = _userInfo
        });

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().NotBeNullOrEmpty();
        // The response should come from Ollama (secondary service) after Gemini fails
    }

    [Fact]
    public async Task Handle_ShouldSearchOnInternetForCookies_WhenIsExplicitRequestToSearch()
    {
        // Arrange
        _userInfo = new Dictionary<string, string> { ["id"] = "toolcalling_cookies" };
        var prompt = "use a função \"search\" para pesquisar 'receita de cookie de baunilha'";
        var expectedString = "Receita de cookie de baunilha: https://www.example.com/cookie-recipe";

        var mock = new Mock<ITool>();
        mock.Setup(d => d.ExecuteAsync(It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(expectedString);

        var toolMediator = _serviceProvider.GetRequiredService<IToolMediator>();
        toolMediator.RegisterTool("search", mock.Object);

        // Act
        var result = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = prompt,
            UserInfo = _userInfo
        });

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().NotBeNullOrEmpty();
        result.Text.Should().Contain(expectedString);
    }

    [Fact]
    public async Task Handle_ShouldSearchOnInternetForCookies_WhenIsImplicitRequestToSearch()
    {
        // Arrange
        _userInfo = new Dictionary<string, string> { ["id"] = "toolcalling_cookies" };
        var prompt = "pesquise pra mim uma receita de cookie de baunilha e calcule pra mim 2+2";
        var expectedString = "Receita de cookie de baunilha: https://www.example.com/cookie-recipe";

        var mock = new Mock<ITool>();
        mock.Setup(d => d.ExecuteAsync(It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(expectedString);

        var toolMediator = _serviceProvider.GetRequiredService<IToolMediator>();
        toolMediator.RegisterTool("search", mock.Object);

        // Act
        var result = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = prompt,
            UserInfo = _userInfo
        });

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().NotBeNullOrEmpty();
        result.Text.Should().Contain(expectedString);
    }
}