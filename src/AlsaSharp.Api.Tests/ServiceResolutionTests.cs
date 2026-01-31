using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using AlsaSharp.Library.Operations.Services;

namespace AlsaSharp.Api.Tests;

[TestFixture]
public class ServiceResolutionTests
{
    private WebApplicationFactory<Program> _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [TearDown]
    public void TearDown()
    {
        _factory?.Dispose();
    }

    [Test]
    public void AllRegisteredServicesShouldResolve()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert - Test all operation services resolve
        Assert.DoesNotThrow(() => services.GetRequiredService<ILoopbackTestOperation>());
        Assert.DoesNotThrow(() => services.GetRequiredService<ISNRTestOperation>());
        Assert.DoesNotThrow(() => services.GetRequiredService<ITestToneOperation>());
        Assert.DoesNotThrow(() => services.GetRequiredService<IAudioLevelsOperation>());
        Assert.DoesNotThrow(() => services.GetRequiredService<ICopyOnlyOperation>());
        Assert.DoesNotThrow(() => services.GetRequiredService<IAudioCardsOperation>());
    }

    [Test]
    public void LoopbackTestOperation_ShouldResolveCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var operation = services.GetRequiredService<ILoopbackTestOperation>();

        // Assert
        Assert.That(operation, Is.Not.Null);
        Assert.That(operation, Is.InstanceOf<ILoopbackTestOperation>());
    }

    [Test]
    public void SNRTestOperation_ShouldResolveCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var operation = services.GetRequiredService<ISNRTestOperation>();

        // Assert
        Assert.That(operation, Is.Not.Null);
        Assert.That(operation, Is.InstanceOf<ISNRTestOperation>());
    }

    [Test]
    public void TestToneOperation_ShouldResolveCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var operation = services.GetRequiredService<ITestToneOperation>();

        // Assert
        Assert.That(operation, Is.Not.Null);
        Assert.That(operation, Is.InstanceOf<ITestToneOperation>());
    }

    [Test]
    public void AudioLevelsOperation_ShouldResolveCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var operation = services.GetRequiredService<IAudioLevelsOperation>();

        // Assert
        Assert.That(operation, Is.Not.Null);
        Assert.That(operation, Is.InstanceOf<IAudioLevelsOperation>());
    }

    [Test]
    public void CopyOnlyOperation_ShouldResolveCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var operation = services.GetRequiredService<ICopyOnlyOperation>();

        // Assert
        Assert.That(operation, Is.Not.Null);
        Assert.That(operation, Is.InstanceOf<ICopyOnlyOperation>());
    }

    [Test]
    public void AudioCardsOperation_ShouldResolveCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var operation = services.GetRequiredService<IAudioCardsOperation>();

        // Assert
        Assert.That(operation, Is.Not.Null);
        Assert.That(operation, Is.InstanceOf<IAudioCardsOperation>());
    }
}
