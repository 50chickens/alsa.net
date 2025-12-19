using System.Runtime.InteropServices;
using AlsaSharp.Core.Native;
using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AlsaSharp.Library.Builders;

/// <summary>
/// Connect your sound device configuration to a virtual interface.
/// </summary>
public class UnixSoundDeviceBuilder
{
    /// <summary>
    /// Build sound device instances using the ALSA hint service (alsahints).
    /// </summary>
    public static IEnumerable<ISoundDevice> Build(IServiceProvider services)
    {
        // Back-compat: call the overload without measurement folder.
        return Build(services, null);
    }

    /// <summary>
    /// Build sound device instances and optionally write baseline summary and per-device header JSON files
    /// into the provided measurement folder. When measurementFolder is null no files are written.
    /// </summary>
    public static IEnumerable<ISoundDevice> Build(IServiceProvider services, string? measurementFolder)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var hintService = services.GetService<IHintService>();
        if (hintService == null)
            throw new InvalidOperationException("IHintService is not registered. Call AddUnixSoundDeviceBuilder() to register hint service before building devices.");

        var log = services.GetService<ILog<UnixSoundDeviceBuilder>>();

        // Prepare measurement folder and timestamp if provided
        string? timestamp = null;
        if (!string.IsNullOrWhiteSpace(measurementFolder))
        {
            measurementFolder = measurementFolder.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            try
            { Directory.CreateDirectory(measurementFolder); }
            catch { }
            timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        var devicesMetadata = new List<object>();
        var list = new List<ISoundDevice>();
        var proberLog = services.GetService<ILog<DeviceProber>>();
        var prober = new DeviceProber(proberLog);

        foreach (var card in hintService.CardInfos)
        {
            var name = card.Id ?? card.Name ?? $"card{card.Index}";
            var soundDeviceSettings = new SoundDeviceSettings
            {
                RecordingDeviceName = $"hw:CARD={name}",
                MixerDeviceName = $"hw:CARD={name}",
                PlaybackDeviceName = $"hw:CARD={name}",
                CardId = card.Id,
                CardName = card.Name,
                CardLongName = card.LongName,
                CardIndex = card.Index
            };

            // Probe device capabilities at build time to populate supported formats, rates and channels.
            try
            {
                prober.Probe(soundDeviceSettings);
            }
            catch (Exception ex)
            {
                log?.Warn($"Failed to probe device {soundDeviceSettings.RecordingDeviceName}: {ex.Message}");
            }

            // If measurement folder provided, compute per-device baseline file path and write header
            if (!string.IsNullOrWhiteSpace(measurementFolder) && timestamp != null)
            {
                var fileBase = SanitizeFileName(soundDeviceSettings.CardName ?? soundDeviceSettings.CardId ?? name);
                var jsonPath = Path.Combine(measurementFolder, $"baseline_{timestamp}_{fileBase}.json");
                soundDeviceSettings.BaselineFilePath = jsonPath;
                try
                {
                    var jsonWriter = new JsonWriter(jsonPath);
                    jsonWriter.Append(new { Device = (object?)null, Card = new { Id = soundDeviceSettings.CardId, Name = soundDeviceSettings.CardName, LongName = soundDeviceSettings.CardLongName, SampleRate = soundDeviceSettings.RecordingSampleRate, BitsPerSample = soundDeviceSettings.RecordingBitsPerSample }, Timestamp = DateTime.UtcNow });
                }
                catch { }
            }

            var logger = services.GetService<ILogger<UnixSoundDevice>>();
            var soundDevice = new UnixSoundDevice(soundDeviceSettings, logger);
            // Log discovered device details here; runtime PCM negotiation produces authoritative params.
            try
            {
                log?.Info($"Discovered device: id={soundDeviceSettings.CardId} name={soundDeviceSettings.CardName} longname={soundDeviceSettings.CardLongName} recording={soundDeviceSettings.RecordingDeviceName} (runtime params negotiated at open)");
            }
            catch { }
            list.Add(soundDevice);
            devicesMetadata.Add(new { soundDeviceSettings.CardId, soundDeviceSettings.CardName, soundDeviceSettings.CardLongName, soundDeviceSettings.RecordingDeviceName, soundDeviceSettings.RecordingSampleRate, soundDeviceSettings.RecordingBitsPerSample, soundDeviceSettings.RecordingChannels });
        }

        // Write a summary JSON into the measurement folder if requested
        if (!string.IsNullOrWhiteSpace(measurementFolder) && timestamp != null)
        {
            try
            {
                var summaryPath = Path.Combine(measurementFolder, $"baseline_summary_{timestamp}.json");
                var summaryWriter = new JsonWriter(summaryPath);
                summaryWriter.Append(new { Timestamp = DateTime.UtcNow, Devices = devicesMetadata });
                log?.Info($"Wrote baseline summary for {devicesMetadata.Count} devices to {summaryPath}");
            }
            catch { }
        }
        return list;
    }

    private static string SanitizeFileName(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "unknown";
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Replace(' ', '_');
    }
}
