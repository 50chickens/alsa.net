using AlsaSharp.Core.Alsa;

namespace AlsaSharp.Core.Tests;

[TestFixture]
public class SubdeviceTests
{
    [Test]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        const int expectedIndex = 0;
        const string expectedName = "Test Subdevice";

        // Act
        var subdevice = new Subdevice(expectedIndex, expectedName);

        // Assert
        Assert.That(subdevice.SubdeviceIndex, Is.EqualTo(expectedIndex));
        Assert.That(subdevice.Name, Is.EqualTo(expectedName));
    }

    [Test]
    public void Constructor_WithNullName_ThrowsInvalidOperationException()
    {
        // Arrange
        const int index = 0;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new Subdevice(index, null!));
    }

    [Test]
    public void Properties_AreReadOnly()
    {
        // Arrange
        var subdevice = new Subdevice(0, "Test");

        // Assert - Properties should not have public setters
        Assert.That(subdevice.SubdeviceIndex, Is.EqualTo(0));
        Assert.That(subdevice.Name, Is.EqualTo("Test"));
    }

    [Test]
    public void Constructor_WithNegativeIndex_AcceptsValue()
    {
        // Arrange & Act
        var subdevice = new Subdevice(-1, "Test");

        // Assert
        Assert.That(subdevice.SubdeviceIndex, Is.EqualTo(-1));
    }

    [Test]
    public void Constructor_WithEmptyName_AcceptsValue()
    {
        // Arrange & Act
        var subdevice = new Subdevice(0, string.Empty);

        // Assert
        Assert.That(subdevice.Name, Is.Empty);
    }
}
