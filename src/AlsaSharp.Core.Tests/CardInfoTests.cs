using AlsaSharp.Core.Alsa;

namespace AlsaSharp.Core.Tests;

[TestFixture]
public class CardInfoTests
{
    [Test]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        const int expectedIndex = 0;
        const string expectedId = "Intel";
        const string expectedName = "HDA Intel";
        const string expectedLongName = "HDA Intel at 0xf7e00000 irq 28";
        const string expectedDriver = "HDA-Intel";
        const string expectedMixerName = "Intel ICH";
        const string expectedComponents = "HDA:10ec0662";

        // Act
        var cardInfo = new CardInfo(
            expectedIndex,
            expectedId,
            expectedName,
            expectedLongName,
            expectedDriver,
            expectedMixerName,
            expectedComponents);

        // Assert
        Assert.That(cardInfo.Index, Is.EqualTo(expectedIndex));
        Assert.That(cardInfo.Id, Is.EqualTo(expectedId));
        Assert.That(cardInfo.Name, Is.EqualTo(expectedName));
        Assert.That(cardInfo.LongName, Is.EqualTo(expectedLongName));
        Assert.That(cardInfo.Driver, Is.EqualTo(expectedDriver));
        Assert.That(cardInfo.MixerName, Is.EqualTo(expectedMixerName));
        Assert.That(cardInfo.Components, Is.EqualTo(expectedComponents));
        Assert.That(cardInfo.PcmEntries, Is.Not.Null);
        Assert.That(cardInfo.PcmEntries, Is.Empty);
    }

    [Test]
    public void Constructor_WithNullId_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new CardInfo(0, null!, "Name", "LongName", "Driver", "Mixer", "Components"));
    }

    [Test]
    public void Constructor_WithNullName_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new CardInfo(0, "Id", null!, "LongName", "Driver", "Mixer", "Components"));
    }

    [Test]
    public void Constructor_WithNullLongName_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new CardInfo(0, "Id", "Name", null!, "Driver", "Mixer", "Components"));
    }

    [Test]
    public void Constructor_WithNullDriver_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new CardInfo(0, "Id", "Name", "LongName", null!, "Mixer", "Components"));
    }

    [Test]
    public void Constructor_WithNullMixerName_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new CardInfo(0, "Id", "Name", "LongName", "Driver", null!, "Components"));
    }

    [Test]
    public void Constructor_WithNullComponents_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new CardInfo(0, "Id", "Name", "LongName", "Driver", "Mixer", null!));
    }

    [Test]
    public void PcmEntries_CanBeModified()
    {
        // Arrange
        var cardInfo = new CardInfo(0, "Id", "Name", "LongName", "Driver", "Mixer", "Components");
        var subdevices = new List<Subdevice> { new Subdevice(0, "Subdevice") };
        var pcmEntry = new PcmEntry(0, "hw:0,0", "Device", subdevices, 1, "PLAYBACK");

        // Act
        cardInfo.PcmEntries.Add(pcmEntry);

        // Assert
        Assert.That(cardInfo.PcmEntries, Has.Count.EqualTo(1));
        Assert.That(cardInfo.PcmEntries[0], Is.EqualTo(pcmEntry));
    }

    [Test]
    public void DeviceIndexes_ReturnsCorrectValues()
    {
        // Arrange
        var cardInfo = new CardInfo(0, "Id", "Name", "LongName", "Driver", "Mixer", "Components");
        var subdevices1 = new List<Subdevice> { new Subdevice(0, "Sub0") };
        var subdevices2 = new List<Subdevice> { new Subdevice(0, "Sub1") };
        cardInfo.PcmEntries.Add(new PcmEntry(0, "hw:0,0", "Device0", subdevices1, 1, "PLAYBACK"));
        cardInfo.PcmEntries.Add(new PcmEntry(1, "hw:0,1", "Device1", subdevices2, 1, "CAPTURE"));

        // Act
        var deviceIndexes = cardInfo.DeviceIndexes;

        // Assert
        Assert.That(deviceIndexes, Has.Count.EqualTo(2));
        Assert.That(deviceIndexes, Contains.Item(0));
        Assert.That(deviceIndexes, Contains.Item(1));
    }

    [Test]
    public void DeviceIds_ReturnsCorrectValues()
    {
        // Arrange
        var cardInfo = new CardInfo(0, "Id", "Name", "LongName", "Driver", "Mixer", "Components");
        var subdevices1 = new List<Subdevice> { new Subdevice(0, "Sub0") };
        var subdevices2 = new List<Subdevice> { new Subdevice(0, "Sub1") };
        cardInfo.PcmEntries.Add(new PcmEntry(0, "hw:0,0", "Device0", subdevices1, 1, "PLAYBACK"));
        cardInfo.PcmEntries.Add(new PcmEntry(1, "hw:0,1", "Device1", subdevices2, 1, "CAPTURE"));

        // Act
        var deviceIds = cardInfo.DeviceIds;

        // Assert
        Assert.That(deviceIds, Has.Count.EqualTo(2));
        Assert.That(deviceIds, Contains.Item("hw:0,0"));
        Assert.That(deviceIds, Contains.Item("hw:0,1"));
    }

    [Test]
    public void DeviceNames_ReturnsCorrectValues()
    {
        // Arrange
        var cardInfo = new CardInfo(0, "Id", "Name", "LongName", "Driver", "Mixer", "Components");
        var subdevices1 = new List<Subdevice> { new Subdevice(0, "Sub0") };
        var subdevices2 = new List<Subdevice> { new Subdevice(0, "Sub1") };
        cardInfo.PcmEntries.Add(new PcmEntry(0, "hw:0,0", "Device0", subdevices1, 1, "PLAYBACK"));
        cardInfo.PcmEntries.Add(new PcmEntry(1, "hw:0,1", "Device1", subdevices2, 1, "CAPTURE"));

        // Act
        var deviceNames = cardInfo.DeviceNames;

        // Assert
        Assert.That(deviceNames, Has.Count.EqualTo(2));
        Assert.That(deviceNames, Contains.Item("Device0"));
        Assert.That(deviceNames, Contains.Item("Device1"));
    }

    [Test]
    public void DeviceSummary_ReturnsCorrectFormat()
    {
        // Arrange
        var cardInfo = new CardInfo(0, "Id", "Name", "LongName", "Driver", "Mixer", "Components");
        var subdevices1 = new List<Subdevice> { new Subdevice(0, "Sub0") };
        var subdevices2 = new List<Subdevice> { new Subdevice(0, "Sub1") };
        cardInfo.PcmEntries.Add(new PcmEntry(0, "hw:0,0", "Device0", subdevices1, 1, "PLAYBACK"));
        cardInfo.PcmEntries.Add(new PcmEntry(1, "hw:0,1", "Device1", subdevices2, 1, "CAPTURE"));

        // Act
        var deviceSummary = cardInfo.DeviceSummary;

        // Assert
        Assert.That(deviceSummary, Is.EqualTo("hw:0,0; hw:0,1"));
    }

    [Test]
    public void DeviceSummary_WithNoPcmEntries_ReturnsEmptyString()
    {
        // Arrange
        var cardInfo = new CardInfo(0, "Id", "Name", "LongName", "Driver", "Mixer", "Components");

        // Act
        var deviceSummary = cardInfo.DeviceSummary;

        // Assert
        Assert.That(deviceSummary, Is.Empty);
    }
}
