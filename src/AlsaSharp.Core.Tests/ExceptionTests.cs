using AlsaSharp.Library;

namespace AlsaSharp.Core.Tests;

[TestFixture]
public class ExceptionTests
{
    [Test]
    public void AlsaDeviceException_InheritsFromException()
    {
        // Assert - Verify type hierarchy
        Assert.That(typeof(AlsaDeviceException).IsSubclassOf(typeof(Exception)), Is.True);
    }

    [Test]
    public void WavFormatException_InheritsFromException()
    {
        // Assert - Verify type hierarchy
        Assert.That(typeof(WavFormatException).IsSubclassOf(typeof(Exception)), Is.True);
    }
}
