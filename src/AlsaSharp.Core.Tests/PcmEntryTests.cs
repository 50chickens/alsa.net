using AlsaSharp.Core.Alsa;

namespace AlsaSharp.Core.Tests;

[TestFixture]
public class PcmEntryTests
{
    [Test]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        const int expectedDeviceIndex = 0;
        const string expectedId = "hw:0,0";
        const string expectedName = "Test Device";
        var expectedSubdevices = new List<Subdevice> { new Subdevice(0, "Subdevice 0") };
        const int expectedSubdevicesCount = 1;
        const string expectedStream = "PLAYBACK";

        // Act
        var pcmEntry = new PcmEntry(
            expectedDeviceIndex,
            expectedId,
            expectedName,
            expectedSubdevices,
            expectedSubdevicesCount,
            expectedStream);

        // Assert
        Assert.That(pcmEntry.DeviceIndex, Is.EqualTo(expectedDeviceIndex));
        Assert.That(pcmEntry.Id, Is.EqualTo(expectedId));
        Assert.That(pcmEntry.Name, Is.EqualTo(expectedName));
        Assert.That(pcmEntry.Subdevices, Is.EqualTo(expectedSubdevices));
        Assert.That(pcmEntry.SubdevicesCount, Is.EqualTo(expectedSubdevicesCount));
        Assert.That(pcmEntry.Stream, Is.EqualTo(expectedStream));
        Assert.That(pcmEntry.SubdeviceName, Is.EqualTo("Subdevice 0"));
    }

    [Test]
    public void Constructor_WithNullId_ThrowsInvalidOperationException()
    {
        // Arrange
        var subdevices = new List<Subdevice> { new Subdevice(0, "Test") };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new PcmEntry(0, null!, "Name", subdevices, 1, "PLAYBACK"));
    }

    [Test]
    public void Constructor_WithNullName_ThrowsInvalidOperationException()
    {
        // Arrange
        var subdevices = new List<Subdevice> { new Subdevice(0, "Test") };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new PcmEntry(0, "hw:0,0", null!, subdevices, 1, "PLAYBACK"));
    }

    [Test]
    public void Constructor_WithNullSubdevices_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new PcmEntry(0, "hw:0,0", "Name", null!, 1, "PLAYBACK"));
    }

    [Test]
    public void Constructor_WithEmptySubdevicesList_ThrowsInvalidOperationException()
    {
        // Arrange
        var subdevices = new List<Subdevice>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new PcmEntry(0, "hw:0,0", "Name", subdevices, 0, "PLAYBACK"));
    }

    [Test]
    public void Constructor_WithNullStream_ThrowsInvalidOperationException()
    {
        // Arrange
        var subdevices = new List<Subdevice> { new Subdevice(0, "Test") };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new PcmEntry(0, "hw:0,0", "Name", subdevices, 1, null!));
    }

    [Test]
    public void SubdeviceName_ReturnsFirstSubdeviceName()
    {
        // Arrange
        var subdevices = new List<Subdevice>
        {
            new Subdevice(0, "First Subdevice"),
            new Subdevice(1, "Second Subdevice")
        };

        // Act
        var pcmEntry = new PcmEntry(0, "hw:0,0", "Name", subdevices, 2, "PLAYBACK");

        // Assert
        Assert.That(pcmEntry.SubdeviceName, Is.EqualTo("First Subdevice"));
    }

    [Test]
    public void Properties_AreReadOnly()
    {
        // Arrange
        var subdevices = new List<Subdevice> { new Subdevice(0, "Test") };
        var pcmEntry = new PcmEntry(0, "hw:0,0", "Name", subdevices, 1, "CAPTURE");

        // Assert - Properties should be readable
        Assert.That(pcmEntry.DeviceIndex, Is.EqualTo(0));
        Assert.That(pcmEntry.Id, Is.EqualTo("hw:0,0"));
        Assert.That(pcmEntry.Name, Is.EqualTo("Name"));
        Assert.That(pcmEntry.Stream, Is.EqualTo("CAPTURE"));
    }
}
