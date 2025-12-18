using AlsaSharp;

namespace AlsaSharp.Core.Tests;

[TestFixture]
public class SoundDeviceSettingsTests
{
    [Test]
    public void Constructor_InitializesDefaultValues()
    {
        // Act
        var settings = new SoundDeviceSettings();

        // Assert
        Assert.That(settings.PlaybackDeviceName, Is.EqualTo("default"));
        Assert.That(settings.RecordingDeviceName, Is.EqualTo("default"));
        Assert.That(settings.MixerDeviceName, Is.EqualTo("default"));
        Assert.That(settings.RecordingSampleRate, Is.EqualTo(48000));
        Assert.That(settings.RecordingChannels, Is.EqualTo(2));
        Assert.That(settings.RecordingBitsPerSample, Is.EqualTo(16));
        Assert.That(settings.CardId, Is.Null);
        Assert.That(settings.CardName, Is.Null);
        Assert.That(settings.CardLongName, Is.Null);
        Assert.That(settings.CardIndex, Is.Null);
        Assert.That(settings.BaselineFilePath, Is.Null);
    }

    [Test]
    public void PlaybackDeviceName_CanBeSet()
    {
        // Arrange
        var settings = new SoundDeviceSettings();
        const string expectedName = "hw:0,0";

        // Act
        settings.PlaybackDeviceName = expectedName;

        // Assert
        Assert.That(settings.PlaybackDeviceName, Is.EqualTo(expectedName));
    }

    [Test]
    public void RecordingDeviceName_CanBeSet()
    {
        // Arrange
        var settings = new SoundDeviceSettings();
        const string expectedName = "hw:1,0";

        // Act
        settings.RecordingDeviceName = expectedName;

        // Assert
        Assert.That(settings.RecordingDeviceName, Is.EqualTo(expectedName));
    }

    [Test]
    public void MixerDeviceName_CanBeSet()
    {
        // Arrange
        var settings = new SoundDeviceSettings();
        const string expectedName = "hw:0";

        // Act
        settings.MixerDeviceName = expectedName;

        // Assert
        Assert.That(settings.MixerDeviceName, Is.EqualTo(expectedName));
    }

    [Test]
    public void RecordingSampleRate_CanBeSet()
    {
        // Arrange
        var settings = new SoundDeviceSettings();
        const uint expectedRate = 44100;

        // Act
        settings.RecordingSampleRate = expectedRate;

        // Assert
        Assert.That(settings.RecordingSampleRate, Is.EqualTo(expectedRate));
    }

    [Test]
    public void RecordingChannels_CanBeSet()
    {
        // Arrange
        var settings = new SoundDeviceSettings();
        const ushort expectedChannels = 1;

        // Act
        settings.RecordingChannels = expectedChannels;

        // Assert
        Assert.That(settings.RecordingChannels, Is.EqualTo(expectedChannels));
    }

    [Test]
    public void RecordingBitsPerSample_CanBeSet()
    {
        // Arrange
        var settings = new SoundDeviceSettings();
        const ushort expectedBits = 24;

        // Act
        settings.RecordingBitsPerSample = expectedBits;

        // Assert
        Assert.That(settings.RecordingBitsPerSample, Is.EqualTo(expectedBits));
    }

    [Test]
    public void CardId_CanBeSet()
    {
        // Arrange
        var settings = new SoundDeviceSettings();
        const string expectedId = "Plus";

        // Act
        settings.CardId = expectedId;

        // Assert
        Assert.That(settings.CardId, Is.EqualTo(expectedId));
    }

    [Test]
    public void CardName_CanBeSet()
    {
        // Arrange
        var settings = new SoundDeviceSettings();
        const string expectedName = "JAM Plus";

        // Act
        settings.CardName = expectedName;

        // Assert
        Assert.That(settings.CardName, Is.EqualTo(expectedName));
    }

    [Test]
    public void CardLongName_CanBeSet()
    {
        // Arrange
        var settings = new SoundDeviceSettings();
        const string expectedLongName = "Apogee JAM Plus at usb-0000:00:14.0-1";

        // Act
        settings.CardLongName = expectedLongName;

        // Assert
        Assert.That(settings.CardLongName, Is.EqualTo(expectedLongName));
    }

    [Test]
    public void CardIndex_CanBeSet()
    {
        // Arrange
        var settings = new SoundDeviceSettings();
        const int expectedIndex = 2;

        // Act
        settings.CardIndex = expectedIndex;

        // Assert
        Assert.That(settings.CardIndex, Is.EqualTo(expectedIndex));
    }

    [Test]
    public void BaselineFilePath_CanBeSet()
    {
        // Arrange
        var settings = new SoundDeviceSettings();
        const string expectedPath = "/path/to/baseline.json";

        // Act
        settings.BaselineFilePath = expectedPath;

        // Assert
        Assert.That(settings.BaselineFilePath, Is.EqualTo(expectedPath));
    }

    [Test]
    public void AllProperties_CanBeSetToNull()
    {
        // Arrange
        var settings = new SoundDeviceSettings
        {
            CardId = "Test",
            CardName = "Test",
            CardLongName = "Test",
            CardIndex = 0,
            BaselineFilePath = "Test"
        };

        // Act
        settings.CardId = null;
        settings.CardName = null;
        settings.CardLongName = null;
        settings.CardIndex = null;
        settings.BaselineFilePath = null;

        // Assert
        Assert.That(settings.CardId, Is.Null);
        Assert.That(settings.CardName, Is.Null);
        Assert.That(settings.CardLongName, Is.Null);
        Assert.That(settings.CardIndex, Is.Null);
        Assert.That(settings.BaselineFilePath, Is.Null);
    }
}
