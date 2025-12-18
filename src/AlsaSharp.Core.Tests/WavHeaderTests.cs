using AlsaSharp.Library;

namespace AlsaSharp.Core.Tests;

[TestFixture]
public class WavHeaderTests
{
    [Test]
    public void Build_WithValidParameters_CreatesCorrectHeader()
    {
        // Arrange
        const uint sampleRate = 48000;
        const ushort channels = 2;
        const ushort bitsPerSample = 16;

        // Act
        var header = WavHeader.Build(sampleRate, channels, bitsPerSample);

        // Assert
        Assert.That(header.ChunkId, Is.EqualTo(new[] { 'R', 'I', 'F', 'F' }));
        Assert.That(header.ChunkSize, Is.EqualTo(0xFFFFFFFF));
        Assert.That(header.Format, Is.EqualTo(new[] { 'W', 'A', 'V', 'E' }));
        Assert.That(header.Subchunk1Id, Is.EqualTo(new[] { 'f', 'm', 't', ' ' }));
        Assert.That(header.Subchunk1Size, Is.EqualTo(16));
        Assert.That(header.AudioFormat, Is.EqualTo(1));
        Assert.That(header.NumChannels, Is.EqualTo(channels));
        Assert.That(header.SampleRate, Is.EqualTo(sampleRate));
        Assert.That(header.BitsPerSample, Is.EqualTo(bitsPerSample));
        Assert.That(header.Subchunk2Id, Is.EqualTo(new[] { 'd', 'a', 't', 'a' }));
        Assert.That(header.Subchunk2Size, Is.EqualTo(0xFFFFFFFF));
    }

    [Test]
    public void Build_CalculatesByteRateCorrectly()
    {
        // Arrange
        const uint sampleRate = 44100;
        const ushort channels = 2;
        const ushort bitsPerSample = 16;
        uint expectedByteRate = sampleRate * channels * bitsPerSample / 8; // 176400

        // Act
        var header = WavHeader.Build(sampleRate, channels, bitsPerSample);

        // Assert
        Assert.That(header.ByteRate, Is.EqualTo(expectedByteRate));
    }

    [Test]
    public void Build_CalculatesBlockAlignCorrectly()
    {
        // Arrange
        const uint sampleRate = 48000;
        const ushort channels = 2;
        const ushort bitsPerSample = 24;
        ushort expectedBlockAlign = (ushort)(channels * bitsPerSample / 8); // 6

        // Act
        var header = WavHeader.Build(sampleRate, channels, bitsPerSample);

        // Assert
        Assert.That(header.BlockAlign, Is.EqualTo(expectedBlockAlign));
    }

    [Test]
    public void Build_WithMonoAudio_CreatesCorrectHeader()
    {
        // Arrange
        const uint sampleRate = 16000;
        const ushort channels = 1;
        const ushort bitsPerSample = 8;

        // Act
        var header = WavHeader.Build(sampleRate, channels, bitsPerSample);

        // Assert
        Assert.That(header.NumChannels, Is.EqualTo(1));
        Assert.That(header.BlockAlign, Is.EqualTo(1));
        Assert.That(header.ByteRate, Is.EqualTo(16000));
    }

    [Test]
    public void Build_With24BitAudio_CreatesCorrectHeader()
    {
        // Arrange
        const uint sampleRate = 96000;
        const ushort channels = 2;
        const ushort bitsPerSample = 24;

        // Act
        var header = WavHeader.Build(sampleRate, channels, bitsPerSample);

        // Assert
        Assert.That(header.BitsPerSample, Is.EqualTo(24));
        Assert.That(header.BlockAlign, Is.EqualTo(6)); // 24 * 2 / 8
        Assert.That(header.ByteRate, Is.EqualTo(576000)); // 96000 * 2 * 24 / 8
    }

    [Test]
    public void WriteToStream_WritesCorrectData()
    {
        // Arrange
        var header = WavHeader.Build(48000, 2, 16);
        using var stream = new MemoryStream();

        // Act
        header.WriteToStream(stream);

        // Assert
        Assert.That(stream.Length, Is.EqualTo(44)); // WAV header is always 44 bytes
        stream.Position = 0;

        // Verify RIFF marker
        var buffer = new byte[4];
        stream.Read(buffer, 0, 4);
        Assert.That(System.Text.Encoding.ASCII.GetString(buffer), Is.EqualTo("RIFF"));
    }

    [Test]
    public void FromStream_ReadsCorrectData()
    {
        // Arrange
        var originalHeader = WavHeader.Build(44100, 2, 16);
        using var stream = new MemoryStream();
        originalHeader.WriteToStream(stream);
        stream.Position = 0;

        // Act
        var readHeader = WavHeader.FromStream(stream);

        // Assert
        Assert.That(readHeader.ChunkId, Is.EqualTo(originalHeader.ChunkId));
        Assert.That(readHeader.Format, Is.EqualTo(originalHeader.Format));
        Assert.That(readHeader.NumChannels, Is.EqualTo(originalHeader.NumChannels));
        Assert.That(readHeader.SampleRate, Is.EqualTo(originalHeader.SampleRate));
        Assert.That(readHeader.BitsPerSample, Is.EqualTo(originalHeader.BitsPerSample));
        Assert.That(readHeader.ByteRate, Is.EqualTo(originalHeader.ByteRate));
        Assert.That(readHeader.BlockAlign, Is.EqualTo(originalHeader.BlockAlign));
    }

    [Test]
    public void WriteAndRead_RoundTrip_PreservesData()
    {
        // Arrange
        var originalHeader = WavHeader.Build(96000, 1, 24);
        using var stream = new MemoryStream();

        // Act
        originalHeader.WriteToStream(stream);
        stream.Position = 0;
        var readHeader = WavHeader.FromStream(stream);

        // Assert
        Assert.That(readHeader.SampleRate, Is.EqualTo(96000));
        Assert.That(readHeader.NumChannels, Is.EqualTo(1));
        Assert.That(readHeader.BitsPerSample, Is.EqualTo(24));
        Assert.That(readHeader.AudioFormat, Is.EqualTo(1));
    }

    [Test]
    public void FromStream_WithInvalidStream_ThrowsWavFormatException()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        // Act & Assert
        Assert.Throws<WavFormatException>(() => WavHeader.FromStream(stream));
    }

    [Test]
    public void WriteToStream_WithNullStream_ThrowsException()
    {
        // Arrange
        var header = WavHeader.Build(48000, 2, 16);

        // Act & Assert
        // Note: The method wraps exceptions in WavFormatException
        Assert.Throws<WavFormatException>(() => header.WriteToStream(null!));
    }
}
