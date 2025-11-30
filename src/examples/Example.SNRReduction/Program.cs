using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Terminal.Gui;
using Alsa.Net;
using Alsa.Net.Internal;
using Alsa.Net.Core;

// Clean, single-version Example.SNRReduction using IConfiguration (appsettings.json)
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddCommandLine(args)
    .Build();

var cfg = configuration.Get<AppConfig>() ?? new AppConfig();
bool baselineOnly = configuration.GetValue<bool>("BaselineOnly") || args.Contains("--baseline");

var log = LogManager.GetLogger<Program>();
log.Info("Starting Example.SNRReduction");

var cards = new AlsaCardEnumerator().GetCards().ToArray();
if (cards.Length == 0) { log.Error("No ALSA cards found"); Environment.Exit(1); }
var chosen = cfg.Card.HasValue ? cards.FirstOrDefault(c => c.Id == cfg.Card.Value) : cards[0];
if (chosen == null) { log.Error($"Card {cfg.Card} not found"); Environment.Exit(1); }

Console.WriteLine($"Using card {chosen.Name} (id={chosen.Id})");
var controls = chosen.GetMixerControls();

var indicesToSweep = (cfg.ControlsToSweep != null && cfg.ControlsToSweep.Length > 0)
    ? cfg.ControlsToSweep
    : Enumerable.Range(0, controls.Length).Where(i => controls[i].Channels.Length > 0).ToArray();

var probe = new MixerProbe();
var settings = new SoundDeviceSettings
{
    PlaybackDeviceName = $"hw:CARD={chosen.Name}",
    RecordingDeviceName = $"hw:CARD={chosen.Name}",
    MixerDeviceName = $"hw:CARD={chosen.Name}",
    RecordingSampleRate = 48000,
    RecordingChannels = 2,
    RecordingBitsPerSample = 16
};

using var device = AlsaDeviceBuilder.Create(settings);

Console.WriteLine("Measuring baseline noise (silence)");
double baselineNoise = MeasureNoise(device, cfg.BaselineSeconds);
Console.WriteLine($"Baseline noise RMS: {baselineNoise:E3}");

Console.WriteLine("Measuring baseline signal (tone)");
double baselineSignal = await MeasureSignalAsync(device, cfg.SignalSeconds, cfg.TestToneHz);
Console.WriteLine($"Baseline signal RMS: {baselineSignal:E3}");

if (baselineOnly)
{
    var baselineResult = new { Card = chosen.Id, CardName = chosen.Name, BaselineNoise = baselineNoise, BaselineSignal = baselineSignal };
    File.WriteAllText(cfg.ResultsFile, JsonSerializer.Serialize(baselineResult, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Wrote baseline results to {cfg.ResultsFile}");
    Environment.Exit(0);
}

if (cfg.ShowGui) ShowGuiMonitor(device, cfg.GuiTimeoutSeconds);

var sweepResults = new List<SweepResult>();
foreach (var idx in indicesToSweep)
{
    if (idx < 0 || idx >= controls.Length) continue;
    var ctrl = controls[idx];
    Console.WriteLine($"Sweeping control [{idx}] {ctrl.ControlName}");

    for (int ch = 0; ch < ctrl.Channels.Length; ch++)
    {
        var chInfo = ctrl.Channels[ch];
        var bestNoise = double.PositiveInfinity;
        nint bestVal = chInfo.Raw;

        for (int s = 0; s <= cfg.Steps; s++)
        {
            var val = chInfo.Min + ((chInfo.Max - chInfo.Min) * s) / Math.Max(1, cfg.Steps);
            nint nval = val;
            bool set = probe.TrySetCaptureVolume(chosen.Id, ctrl.ControlName, chInfo.Name, nval);
            if (!set) set = probe.TrySetPlaybackVolume(chosen.Id, ctrl.ControlName, chInfo.Name, nval);
            if (!set) continue;

            await Task.Delay(cfg.SettleMs);
            var noise = MeasureNoise(device, cfg.BaselineSeconds);
            var signal = await MeasureSignalAsync(device, cfg.SignalSeconds, cfg.TestToneHz);

            double signalDb = 20 * Math.Log10(Math.Max(signal, 1e-12));
            double baselineSignalDb = 20 * Math.Log10(Math.Max(baselineSignal, 1e-12));
            if (signalDb < baselineSignalDb - cfg.MaxSignalDropDb) continue;

            if (noise < bestNoise) { bestNoise = noise; bestVal = nval; }
        }

        sweepResults.Add(new SweepResult { ControlIndex = idx, ChannelIndex = ch, BestValue = (long)bestVal, BestNoise = bestNoise, BaselineNoise = baselineNoise, BaselineSignal = baselineSignal });
        if (!double.IsPositiveInfinity(bestNoise))
        {
            probe.TrySetCaptureVolume(chosen.Id, ctrl.ControlName, chInfo.Name, bestVal);
            probe.TrySetPlaybackVolume(chosen.Id, ctrl.ControlName, chInfo.Name, bestVal);
            Console.WriteLine($"Applied best value {bestVal} for control {ctrl.ControlName} channel {chInfo.Name}");
        }
    }
}

File.WriteAllText(cfg.ResultsFile, JsonSerializer.Serialize(new { Card = chosen.Id, CardName = chosen.Name, Results = sweepResults }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Wrote sweep results to {cfg.ResultsFile}");

// --- helpers/types ---
class AppConfig
{
    public int? Card { get; set; }
    public int[] ControlsToSweep { get; set; } = Array.Empty<int>();
    public int Steps { get; set; } = 5;
    public int BaselineSeconds { get; set; } = 3;
    public int SignalSeconds { get; set; } = 3;
    public int TestToneHz { get; set; } = 1000;
    public string ResultsFile { get; set; } = "snr_results.json";
    public bool ShowGui { get; set; } = true;
    public int GuiTimeoutSeconds { get; set; } = 30;
    public int SettleMs { get; set; } = 200;
    public double MaxSignalDropDb { get; set; } = 1.0;
}

class SweepResult { public int ControlIndex { get; set; } public int ChannelIndex { get; set; } public long BestValue { get; set; } public double BestNoise { get; set; } public double BaselineNoise { get; set; } public double BaselineSignal { get; set; } }

static double MeasureNoise(ISoundDevice device, int seconds)
{
    var rmsList = new List<double>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 1));
    bool headerSeen = false;
    void OnData(byte[] buffer)
    {
        if (!headerSeen) { headerSeen = true; return; }
        int bytesPerSample = 2, channels = 2;
        int frameCount = buffer.Length / (bytesPerSample * channels);
        if (frameCount <= 0) return;
        long sumSqL = 0, sumSqR = 0; int samples = 0;
        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * channels * bytesPerSample;
            if (offset + 3 >= buffer.Length) break;
            short sL = BitConverter.ToInt16(buffer, offset);
            short sR = BitConverter.ToInt16(buffer, offset + 2);
            sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
        }
        if (samples == 0) return;
        double rms = Math.Sqrt((sumSqL + sumSqR) / (double)(samples * 2)) / 32768.0;
        rmsList.Add(rms);
    }

    var task = Task.Run(() => device.Record(OnData, cts.Token));
    try { task.Wait(cts.Token); } catch { }
    return rmsList.Count == 0 ? 0.0 : rmsList.Average();
}

static async Task<double> MeasureSignalAsync(ISoundDevice device, int seconds, double freq)
{
    int sampleRate = 48000, channels = 2, bits = 16;
    var tone = GenerateToneWav(sampleRate, channels, bits, freq, seconds, 0.5);
    var rmsList = new List<double>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 2));
    bool headerSeen = false;
    void OnData(byte[] buffer)
    {
        if (!headerSeen) { headerSeen = true; return; }
        int bytesPerSample = 2, chs = 2;
        int frameCount = buffer.Length / (bytesPerSample * chs);
        if (frameCount <= 0) return;
        long sumSqL = 0, sumSqR = 0; int samples = 0;
        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * chs * bytesPerSample;
            if (offset + 3 >= buffer.Length) break;
            short sL = BitConverter.ToInt16(buffer, offset);
            short sR = BitConverter.ToInt16(buffer, offset + 2);
            sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
        }
        if (samples == 0) return;
        double rms = Math.Sqrt((sumSqL + sumSqR) / (double)(samples * 2)) / 32768.0;
        rmsList.Add(rms);
    }

    var recordTask = Task.Run(() => device.Record(OnData, cts.Token));
    device.Play(new MemoryStream(tone), cts.Token);
    try { await recordTask; } catch { }
    return rmsList.Count == 0 ? 0.0 : rmsList.Average();
}

static byte[] GenerateToneWav(int sampleRate, int channels, int bitsPerSample, double freq, int seconds, double amplitude)
{
    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);
    int byteRate = sampleRate * channels * bitsPerSample / 8;
    int blockAlign = channels * bitsPerSample / 8;
    bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
    bw.Write(36 + sampleRate * seconds * blockAlign);
    bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
    bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
    bw.Write(16);
    bw.Write((short)1);
    bw.Write((short)channels);
    bw.Write(sampleRate);
    bw.Write(byteRate);
    bw.Write((short)blockAlign);
    bw.Write((short)bitsPerSample);
    bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
    int dataSize = sampleRate * seconds * blockAlign;
    bw.Write(dataSize);
    int totalFrames = sampleRate * seconds;
    for (int i = 0; i < totalFrames; i++)
    {
        double t = i / (double)sampleRate;
        double s = Math.Sin(2.0 * Math.PI * freq * t) * amplitude;
        short sample = (short)(s * short.MaxValue);
        for (int ch = 0; ch < channels; ch++) bw.Write(sample);
    }
    return ms.ToArray();
}

static void ShowGuiMonitor(ISoundDevice device, int timeoutSeconds)
{
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
    var queue = new System.Collections.Concurrent.ConcurrentQueue<(double l, double r)>();
    void OnData(byte[] buffer)
    {
        int bytesPerSample = 2, channels = 2;
        int frameCount = buffer.Length / (bytesPerSample * channels);
        if (frameCount <= 0) return;
        long sumSqL = 0, sumSqR = 0; int samples = 0;
        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * channels * bytesPerSample;
            if (offset + 3 >= buffer.Length) break;
            short sL = BitConverter.ToInt16(buffer, offset);
            short sR = BitConverter.ToInt16(buffer, offset + 2);
            sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
        }
        if (samples == 0) return;
        double rmsL = Math.Sqrt(sumSqL / (double)samples) / 32768.0;
        double rmsR = Math.Sqrt(sumSqR / (double)samples) / 32768.0;
        queue.Enqueue((rmsL, rmsR));
        while (queue.Count > 10) queue.TryDequeue(out _);
    }

    var recordTask = Task.Run(() => device.Record(OnData, cts.Token));
    Application.Init();
    var top = Application.Top;
    var win = new Window("dBFS Monitor") { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
    var lbl = new Label("Initializing...") { X = 0, Y = 0, Width = Dim.Fill(), Height = 1 };
    win.Add(lbl); top.Add(win);
    var uiToken = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
    Task.Run(async () =>
    {
        while (!uiToken.IsCancellationRequested)
        {
            if (queue.TryPeek(out var v))
            {
                var dbL = 20 * Math.Log10(Math.Max(v.l, 1e-12));
                var dbR = 20 * Math.Log10(Math.Max(v.r, 1e-12));
                Application.MainLoop.Invoke(() => lbl.Text = $"L={dbL:F1} dBFS  R={dbR:F1} dBFS");
            }
            await Task.Delay(250);
        }
        Application.MainLoop.Invoke(() => Application.RequestStop());
    });
    Application.Run();
    try { recordTask.Wait(1000); } catch { }
}
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Terminal.Gui;
using Alsa.Net;
using Alsa.Net.Internal;
using Alsa.Net.Core;

// Example.SNRReduction - IConfiguration-based non-interactive autosweep.
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddCommandLine(args)
    .Build();

var config = configuration.Get<AppConfig>() ?? new AppConfig();
bool baselineOnly = configuration.GetValue<bool>("BaselineOnly") || args.Contains("--baseline");

var log = LogManager.GetLogger<Program>();
log.Info("Starting Example.SNRReduction (IConfiguration)");

var cards = new AlsaCardEnumerator().GetCards().ToArray();
if (cards.Length == 0) { log.Error("No ALSA cards found"); Environment.Exit(1); }
var chosen = config.Card.HasValue ? cards.FirstOrDefault(c => c.Id == config.Card.Value) : cards[0];
if (chosen == null) { log.Error($"Card {config.Card} not found"); Environment.Exit(1); }

Console.WriteLine($"Using card {chosen.Name} (id={chosen.Id})");
var controls = chosen.GetMixerControls();

var indicesToSweep = (config.ControlsToSweep != null && config.ControlsToSweep.Length > 0)
    ? config.ControlsToSweep
    : Enumerable.Range(0, controls.Length).Where(i => controls[i].Channels.Length > 0).ToArray();

var probe = new MixerProbe();
var settings = new SoundDeviceSettings
{
    PlaybackDeviceName = $"hw:CARD={chosen.Name}",
    RecordingDeviceName = $"hw:CARD={chosen.Name}",
    MixerDeviceName = $"hw:CARD={chosen.Name}",
    RecordingSampleRate = 48000,
    RecordingChannels = 2,
    RecordingBitsPerSample = 16
};

using var device = AlsaDeviceBuilder.Create(settings);

Console.WriteLine("Measuring baseline noise (silence)");
double baselineNoise = MeasureNoise(device, config.BaselineSeconds);
Console.WriteLine($"Baseline noise RMS: {baselineNoise:E3}");

Console.WriteLine("Measuring baseline signal (tone)");
double baselineSignal = await MeasureSignalAsync(device, config.SignalSeconds, config.TestToneHz);
Console.WriteLine($"Baseline signal RMS: {baselineSignal:E3}");

if (baselineOnly)
{
    var baselineResult = new { Card = chosen.Id, CardName = chosen.Name, BaselineNoise = baselineNoise, BaselineSignal = baselineSignal };
    File.WriteAllText(config.ResultsFile, JsonSerializer.Serialize(baselineResult, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Wrote baseline results to {config.ResultsFile}");
    Environment.Exit(0);
}

if (config.ShowGui) ShowGuiMonitor(device, config.GuiTimeoutSeconds);

var sweepResults = new List<SweepResult>();
foreach (var idx in indicesToSweep)
{
    if (idx < 0 || idx >= controls.Length) continue;
    var ctrl = controls[idx];
    Console.WriteLine($"Sweeping control [{idx}] {ctrl.ControlName}");

    for (int ch = 0; ch < ctrl.Channels.Length; ch++)
    {
        var chInfo = ctrl.Channels[ch];
        var bestNoise = double.PositiveInfinity;
        nint bestVal = chInfo.Raw;

        for (int s = 0; s <= config.Steps; s++)
        {
            var val = chInfo.Min + ((chInfo.Max - chInfo.Min) * s) / Math.Max(1, config.Steps);
            nint nval = val;
            bool set = probe.TrySetCaptureVolume(chosen.Id, ctrl.ControlName, chInfo.Name, nval);
            if (!set) set = probe.TrySetPlaybackVolume(chosen.Id, ctrl.ControlName, chInfo.Name, nval);
            if (!set) continue;

            await Task.Delay(config.SettleMs);
            var noise = MeasureNoise(device, config.BaselineSeconds);
            var signal = await MeasureSignalAsync(device, config.SignalSeconds, config.TestToneHz);

            double signalDb = 20 * Math.Log10(Math.Max(signal, 1e-12));
            double baselineSignalDb = 20 * Math.Log10(Math.Max(baselineSignal, 1e-12));
            if (signalDb < baselineSignalDb - config.MaxSignalDropDb) continue;

            if (noise < bestNoise) { bestNoise = noise; bestVal = nval; }
        }

        sweepResults.Add(new SweepResult { ControlIndex = idx, ChannelIndex = ch, BestValue = (long)bestVal, BestNoise = bestNoise, BaselineNoise = baselineNoise, BaselineSignal = baselineSignal });
        if (!double.IsPositiveInfinity(bestNoise))
        {
            probe.TrySetCaptureVolume(chosen.Id, ctrl.ControlName, chInfo.Name, bestVal);
            probe.TrySetPlaybackVolume(chosen.Id, ctrl.ControlName, chInfo.Name, bestVal);
            Console.WriteLine($"Applied best value {bestVal} for control {ctrl.ControlName} channel {chInfo.Name}");
        }
    }
}

File.WriteAllText(config.ResultsFile, JsonSerializer.Serialize(new { Card = chosen.Id, CardName = chosen.Name, Results = sweepResults }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Wrote sweep results to {config.ResultsFile}");

// helpers and types
class AppConfig
{
    public int? Card { get; set; }
    public int[] ControlsToSweep { get; set; } = Array.Empty<int>();
    public int Steps { get; set; } = 5;
    public int BaselineSeconds { get; set; } = 3;
    public int SignalSeconds { get; set; } = 3;
    public int TestToneHz { get; set; } = 1000;
    public string ResultsFile { get; set; } = "snr_results.json";
    public bool ShowGui { get; set; } = true;
    public int GuiTimeoutSeconds { get; set; } = 60;
    public int SettleMs { get; set; } = 200;
    public double MaxSignalDropDb { get; set; } = 1.0;
}

class SweepResult { public int ControlIndex { get; set; } public int ChannelIndex { get; set; } public long BestValue { get; set; } public double BestNoise { get; set; } public double BaselineNoise { get; set; } public double BaselineSignal { get; set; } }

static double MeasureNoise(ISoundDevice device, int seconds)
{
    var rmsList = new List<double>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 1));
    bool headerSeen = false;
    void OnData(byte[] buffer)
    {
        if (!headerSeen) { headerSeen = true; return; }
        int bytesPerSample = 2, channels = 2;
        int frameCount = buffer.Length / (bytesPerSample * channels);
        if (frameCount <= 0) return;
        long sumSqL = 0, sumSqR = 0; int samples = 0;
        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * channels * bytesPerSample;
            if (offset + 3 >= buffer.Length) break;
            short sL = BitConverter.ToInt16(buffer, offset);
            short sR = BitConverter.ToInt16(buffer, offset + 2);
            sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
        }
        if (samples == 0) return;
        double rms = Math.Sqrt((sumSqL + sumSqR) / (double)(samples * 2)) / 32768.0;
        rmsList.Add(rms);
    }

    var task = Task.Run(() => device.Record(OnData, cts.Token));
    try { task.Wait(cts.Token); } catch { }
    return rmsList.Count == 0 ? 0.0 : rmsList.Average();
}

static async Task<double> MeasureSignalAsync(ISoundDevice device, int seconds, double freq)
{
    int sampleRate = 48000, channels = 2, bits = 16;
    var tone = GenerateToneWav(sampleRate, channels, bits, freq, seconds, 0.5);
    var rmsList = new List<double>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 2));
    bool headerSeen = false;
    void OnData(byte[] buffer)
    {
        if (!headerSeen) { headerSeen = true; return; }
        int bytesPerSample = 2, chs = 2;
        int frameCount = buffer.Length / (bytesPerSample * chs);
        if (frameCount <= 0) return;
        long sumSqL = 0, sumSqR = 0; int samples = 0;
        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * chs * bytesPerSample;
            if (offset + 3 >= buffer.Length) break;
            short sL = BitConverter.ToInt16(buffer, offset);
            short sR = BitConverter.ToInt16(buffer, offset + 2);
            sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
        }
        if (samples == 0) return;
        double rms = Math.Sqrt((sumSqL + sumSqR) / (double)(samples * 2)) / 32768.0;
        rmsList.Add(rms);
    }

    var recordTask = Task.Run(() => device.Record(OnData, cts.Token));
    device.Play(new MemoryStream(tone), cts.Token);
    try { await recordTask; } catch { }
    return rmsList.Count == 0 ? 0.0 : rmsList.Average();
}

static byte[] GenerateToneWav(int sampleRate, int channels, int bitsPerSample, double freq, int seconds, double amplitude)
{
    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);
    int byteRate = sampleRate * channels * bitsPerSample / 8;
    int blockAlign = channels * bitsPerSample / 8;
    bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
    bw.Write(36 + sampleRate * seconds * blockAlign);
    bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
    bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
    bw.Write(16);
    bw.Write((short)1);
    bw.Write((short)channels);
    bw.Write(sampleRate);
    bw.Write(byteRate);
    bw.Write((short)blockAlign);
    bw.Write((short)bitsPerSample);
    bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
    int dataSize = sampleRate * seconds * blockAlign;
    bw.Write(dataSize);
    int totalFrames = sampleRate * seconds;
    for (int i = 0; i < totalFrames; i++)
    {
        double t = i / (double)sampleRate;
        double s = Math.Sin(2.0 * Math.PI * freq * t) * amplitude;
        short sample = (short)(s * short.MaxValue);
        for (int ch = 0; ch < channels; ch++) bw.Write(sample);
    }
    return ms.ToArray();
}

static void ShowGuiMonitor(ISoundDevice device, int timeoutSeconds)
{
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
    var queue = new System.Collections.Concurrent.ConcurrentQueue<(double l, double r)>();
    void OnData(byte[] buffer)
    {
        int bytesPerSample = 2, channels = 2;
        int frameCount = buffer.Length / (bytesPerSample * channels);
        if (frameCount <= 0) return;
        long sumSqL = 0, sumSqR = 0; int samples = 0;
        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * channels * bytesPerSample;
            if (offset + 3 >= buffer.Length) break;
            short sL = BitConverter.ToInt16(buffer, offset);
            short sR = BitConverter.ToInt16(buffer, offset + 2);
            sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
        }
        if (samples == 0) return;
        double rmsL = Math.Sqrt(sumSqL / (double)samples) / 32768.0;
        double rmsR = Math.Sqrt(sumSqR / (double)samples) / 32768.0;
        queue.Enqueue((rmsL, rmsR));
        while (queue.Count > 10) queue.TryDequeue(out _);
    }

    var recordTask = Task.Run(() => device.Record(OnData, cts.Token));
    Application.Init();
    var top = Application.Top;
    var win = new Window("dBFS Monitor") { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
    var lbl = new Label("Initializing...") { X = 0, Y = 0, Width = Dim.Fill(), Height = 1 };
    win.Add(lbl); top.Add(win);
    var uiToken = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
    Task.Run(async () =>
    {
        while (!uiToken.IsCancellationRequested)
        {
            if (queue.TryPeek(out var v))
            {
                var dbL = 20 * Math.Log10(Math.Max(v.l, 1e-12));
                var dbR = 20 * Math.Log10(Math.Max(v.r, 1e-12));
                Application.MainLoop.Invoke(() => lbl.Text = $"L={dbL:F1} dBFS  R={dbR:F1} dBFS");
            }
            await Task.Delay(250);
        }
        Application.MainLoop.Invoke(() => Application.RequestStop());
    });
    Application.Run();
    try { recordTask.Wait(1000); } catch { }
}

    var recordTask = Task.Run(() => device.Record(OnData, cts.Token));

    Application.Init();
    var top = Application.Top;
    var win = new Window("dBFS Monitor") { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
    var lbl = new Label("Initializing...") { X = 0, Y = 0, Width = Dim.Fill(), Height = 1 };
    win.Add(lbl);
    top.Add(win);

    var uiToken = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
    Task.Run(async () =>
    {
        while (!uiToken.IsCancellationRequested)
        {
            if (queue.TryPeek(out var v))
            {
                var dbL = 20 * Math.Log10(Math.Max(v.l, 1e-12));
                var dbR = 20 * Math.Log10(Math.Max(v.r, 1e-12));
                Application.MainLoop.Invoke(() => lbl.Text = $"L={dbL:F1} dBFS  R={dbR:F1} dBFS");
            }
            await Task.Delay(250);
        }
        Application.MainLoop.Invoke(() => Application.RequestStop());
    });

    Application.Run();
    try { recordTask.Wait(1000); } catch { }
}
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Terminal.Gui;
using Alsa.Net;
using Alsa.Net.Internal;
using Alsa.Net.Core;

// Config-driven non-interactive autosweep and baseline measurement.
// Reads `appsettings.json` from current working directory by default.

var configPath = "appsettings.json";
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--config" && i + 1 < args.Length) configPath = args[++i];
}

var config = LoadConfig(configPath);
bool baselineOnly = args.Contains("--baseline");

var log = LogManager.GetLogger<Program>();
log.Info("Starting Example.SNRReduction (non-interactive)");

// discover cards
var enumerator = new AlsaCardEnumerator();
var cards = enumerator.GetCards().ToArray();
if (cards.Length == 0)
{
    log.Error("No ALSA cards found");
    Environment.Exit(1);
}

var chosen = config.Card.HasValue ? cards.FirstOrDefault(c => c.Id == config.Card.Value) : cards[0];
if (chosen == null)
{
    log.Error($"Card {config.Card} not found");
    Environment.Exit(1);
}
log.Info($"Using card {chosen.Name} (id={chosen.Id})");

// enumerate controls
var controls = chosen.GetMixerControls();
log.Info($"Found {controls.Length} controls for card '{chosen.Name}'");

// If no explicit controls specified, default to all controls that have channels
var indicesToSweep = config.ControlsToSweep != null && config.ControlsToSweep.Length > 0
    ? config.ControlsToSweep
    : Enumerable.Range(0, controls.Length).Where(i => controls[i].Channels.Length > 0).ToArray();

// Baseline measurement (silence + tone)
var probe = new MixerProbe();
var settings = new SoundDeviceSettings
{
    PlaybackDeviceName = $"hw:CARD={chosen.Name}",
    RecordingDeviceName = $"hw:CARD={chosen.Name}",
    MixerDeviceName = $"hw:CARD={chosen.Name}",
    RecordingSampleRate = 48000,
    RecordingChannels = 2,
    RecordingBitsPerSample = 16
};

using var device = AlsaDeviceBuilder.Create(settings);

Console.WriteLine("Measuring baseline noise (silence)");
double baselineNoise = MeasureNoise(device, config.BaselineSeconds);
Console.WriteLine($"Baseline noise RMS: {baselineNoise:E3}");

Console.WriteLine("Measuring baseline signal (tone)");
double baselineSignal = await MeasureSignalAsync(device, config.SignalSeconds, 1000);
Console.WriteLine($"Baseline signal RMS: {baselineSignal:E3}");

if (baselineOnly)
{
    var baselineResult = new { Card = chosen.Id, CardName = chosen.Name, BaselineNoise = baselineNoise, BaselineSignal = baselineSignal };
    File.WriteAllText(config.ResultsFile, JsonSerializer.Serialize(baselineResult, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Wrote baseline results to {config.ResultsFile}");
    Environment.Exit(0);
}

// Optional Terminal.Gui monitor phase for a short timeout so user can observe dBFS
if (config.ShowGui)
{
    Console.WriteLine($"Showing Terminal.Gui dBFS monitor for {config.GuiTimeoutSeconds} seconds...");
    ShowGuiMonitor(device, config.GuiTimeoutSeconds);
}

// Autosweep
var sweepResults = new List<SweepResult>();
foreach (var idx in indicesToSweep)
{
    if (idx < 0 || idx >= controls.Length) continue;
    var ctrl = controls[idx];
    Console.WriteLine($"Sweeping control [{idx}] {ctrl.ControlName}");

    for (int ch = 0; ch < ctrl.Channels.Length; ch++)
    {
        var chInfo = ctrl.Channels[ch];
        var bestNoise = double.PositiveInfinity;
        nint bestVal = chInfo.Raw;

        for (int s = 0; s <= config.Steps; s++)
        {
            var val = chInfo.Min + ((chInfo.Max - chInfo.Min) * s) / Math.Max(1, config.Steps);
            nint nval = val;

            bool set = probe.TrySetCaptureVolume(chosen.Id, ctrl.ControlName, chInfo.Name, nval);
            if (!set) set = probe.TrySetPlaybackVolume(chosen.Id, ctrl.ControlName, chInfo.Name, nval);
            if (!set) continue;

            await Task.Delay(200);

            var noise = MeasureNoise(device, config.BaselineSeconds);
            var signal = await MeasureSignalAsync(device, config.SignalSeconds, 1000);

            double signalDb = 20 * Math.Log10(Math.Max(signal, 1e-12));
            double baselineSignalDb = 20 * Math.Log10(Math.Max(baselineSignal, 1e-12));
            if (signalDb < baselineSignalDb - 1.0) continue; // signal dropped too much

            if (noise < bestNoise)
            {
                bestNoise = noise;
                bestVal = nval;
            }
        }

        sweepResults.Add(new SweepResult
        {
            ControlIndex = idx,
            ChannelIndex = ch,
            BestValue = (long)bestVal,
            BestNoise = bestNoise,
            BaselineNoise = baselineNoise,
            BaselineSignal = baselineSignal
        });

        // apply best if found
        if (!double.IsPositiveInfinity(bestNoise))
        {
            probe.TrySetCaptureVolume(chosen.Id, ctrl.ControlName, chInfo.Name, bestVal);
            probe.TrySetPlaybackVolume(chosen.Id, ctrl.ControlName, chInfo.Name, bestVal);
            Console.WriteLine($"Applied best value {bestVal} for control {ctrl.ControlName} channel {chInfo.Name}");
        }
    }
}

// Write results
var outObj = new { Card = chosen.Id, CardName = chosen.Name, Results = sweepResults };
File.WriteAllText(config.ResultsFile, JsonSerializer.Serialize(outObj, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Wrote sweep results to {config.ResultsFile}");

// Exit
Environment.Exit(0);

// ----------------- Helpers and types -----------------
class AppConfig
{
    public int? Card { get; set; }
    public int[] ControlsToSweep { get; set; } = Array.Empty<int>();
    public int Steps { get; set; } = 5;
    public int BaselineSeconds { get; set; } = 3;
    public int SignalSeconds { get; set; } = 3;
    public string ResultsFile { get; set; } = "results.json";
    public bool ShowGui { get; set; } = true;
    public int GuiTimeoutSeconds { get; set; } = 60;
}

class SweepResult
{
    public int ControlIndex { get; set; }
    public int ChannelIndex { get; set; }
    public long BestValue { get; set; }
    public double BestNoise { get; set; }
    public double BaselineNoise { get; set; }
    public double BaselineSignal { get; set; }
}

static AppConfig LoadConfig(string path)
{
    try
    {
        if (!File.Exists(path)) return new AppConfig();
        var txt = File.ReadAllText(path);
        var cfg = JsonSerializer.Deserialize<AppConfig>(txt, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return cfg ?? new AppConfig();
    }
    catch
    {
        return new AppConfig();
    }
}

static double MeasureNoise(ISoundDevice device, int seconds)
{
    var rmsList = new List<double>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 1));
    bool headerSeen = false;
    void OnData(byte[] buffer)
    {
        if (!headerSeen) { headerSeen = true; return; }
        int bytesPerSample = 2; int channels = 2;
        int frameCount = buffer.Length / (bytesPerSample * channels);
        if (frameCount <= 0) return;
        long sumSqL = 0, sumSqR = 0; int samples = 0;
        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * channels * bytesPerSample;
            if (offset + 3 >= buffer.Length) break;
            short sL = BitConverter.ToInt16(buffer, offset);
            short sR = BitConverter.ToInt16(buffer, offset + 2);
            sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
        }
        if (samples == 0) return;
        double rms = Math.Sqrt((sumSqL + sumSqR) / (double)(samples * 2)) / 32768.0;
        rmsList.Add(rms);
    }

    var task = Task.Run(() => device.Record(OnData, cts.Token));
    try { task.Wait(cts.Token); } catch { }
    return rmsList.Count == 0 ? 0.0 : rmsList.Average();
}

static async Task<double> MeasureSignalAsync(ISoundDevice device, int seconds, double freq)
{
    int sampleRate = 48000; int channels = 2; int bits = 16;
    var tone = GenerateToneWav(sampleRate, channels, bits, freq, seconds, 0.5);
    var rmsList = new List<double>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 2));
    bool headerSeen = false;
    void OnData(byte[] buffer)
    {
        if (!headerSeen) { headerSeen = true; return; }
        int bytesPerSample = 2; int chs = 2;
        int frameCount = buffer.Length / (bytesPerSample * chs);
        if (frameCount <= 0) return;
        long sumSqL = 0, sumSqR = 0; int samples = 0;
        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * chs * bytesPerSample;
            if (offset + 3 >= buffer.Length) break;
            short sL = BitConverter.ToInt16(buffer, offset);
            short sR = BitConverter.ToInt16(buffer, offset + 2);
            sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
        }
        if (samples == 0) return;
        double rms = Math.Sqrt((sumSqL + sumSqR) / (double)(samples * 2)) / 32768.0;
        rmsList.Add(rms);
    }

    var recordTask = Task.Run(() => device.Record(OnData, cts.Token));
    // play tone
    device.Play(new MemoryStream(tone), cts.Token);
    try { await recordTask; } catch { }
    return rmsList.Count == 0 ? 0.0 : rmsList.Average();
}

static byte[] GenerateToneWav(int sampleRate, int channels, int bitsPerSample, double freq, int seconds, double amplitude)
{
    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);

    int byteRate = sampleRate * channels * bitsPerSample / 8;
    int blockAlign = channels * bitsPerSample / 8;

    // RIFF header
    bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
    bw.Write(36 + sampleRate * seconds * blockAlign); // ChunkSize (approx)
    bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

    // fmt subchunk
    bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
    bw.Write(16); // Subchunk1Size
    bw.Write((short)1); // AudioFormat PCM
    bw.Write((short)channels);
    bw.Write(sampleRate);
    bw.Write(byteRate);
    bw.Write((short)blockAlign);
    bw.Write((short)bitsPerSample);

    // data subchunk header
    bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
    int dataSize = sampleRate * seconds * blockAlign;
    bw.Write(dataSize);

    int totalFrames = sampleRate * seconds;
    for (int i = 0; i < totalFrames; i++)
    {
        double t = i / (double)sampleRate;
        double s = Math.Sin(2.0 * Math.PI * freq * t) * amplitude;
        short sample = (short)(s * short.MaxValue);
        for (int ch = 0; ch < channels; ch++) bw.Write(sample);
    }

    return ms.ToArray();
}

static void ShowGuiMonitor(ISoundDevice device, int timeoutSeconds)
{
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
    var queue = new System.Collections.Concurrent.ConcurrentQueue<(double l, double r)>();

    void OnData(byte[] buffer)
    {
        int bytesPerSample = 2; int channels = 2;
        int frameCount = buffer.Length / (bytesPerSample * channels);
        if (frameCount <= 0) return;
        long sumSqL = 0, sumSqR = 0; int samples = 0;
        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * channels * bytesPerSample;
            if (offset + 3 >= buffer.Length) break;
            short sL = BitConverter.ToInt16(buffer, offset);
            short sR = BitConverter.ToInt16(buffer, offset + 2);
            sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
        }
        if (samples == 0) return;
        double rmsL = Math.Sqrt(sumSqL / (double)samples) / 32768.0;
        double rmsR = Math.Sqrt(sumSqR / (double)samples) / 32768.0;
        queue.Enqueue((rmsL, rmsR));
        while (queue.Count > 10) queue.TryDequeue(out _);
    }

    var recordTask = Task.Run(() => device.Record(OnData, cts.Token));

    Application.Init();
    var top = Application.Top;
    var win = new Window("dBFS Monitor") { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
    var lbl = new Label("Initializing...") { X = 0, Y = 0, Width = Dim.Fill(), Height = 1 };
    win.Add(lbl);
    top.Add(win);

    var uiToken = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
    Task.Run(async () =>
    {
        while (!uiToken.IsCancellationRequested)
        {
            if (queue.TryPeek(out var v))
            {
                var dbL = 20 * Math.Log10(Math.Max(v.l, 1e-12));
                var dbR = 20 * Math.Log10(Math.Max(v.r, 1e-12));
                Application.MainLoop.Invoke(() => lbl.Text = $"L={dbL:F1} dBFS  R={dbR:F1} dBFS");
            }
            await Task.Delay(250);
        }
        Application.MainLoop.Invoke(() => Application.RequestStop());
    });

    Application.Run();
    try { recordTask.Wait(1000); } catch { }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Alsa.Net;
using Alsa.Net.Internal;
using Alsa.Net.Core;
using System.Text.Json;
using Terminal.Gui;

// Simple console app to inspect controls and live-read levels + adjust gains.
// Usage: Example.SNRReduction [--card 0]

// Configuration-driven run. Reads `appsettings.json` in the working directory.
var config = LoadConfig("appsettings.json");

// CLI flags: --baseline to only record baseline, --config <path> to override file
bool baselineOnly = args.Contains("--baseline");
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--config" && i + 1 < args.Length) config = LoadConfig(args[++i]);
    if (args[i] == "--baseline") baselineOnly = true;
}

await RunAsync(config, baselineOnly);

async Task RunAsync(AppConfig config, bool baselineOnly)
{
    var log = LogManager.GetLogger<Program>();
    log.Info("Starting Example.SNRReduction");

    // discover cards
    var enumerator = new AlsaCardEnumerator();
    var cards = enumerator.GetCards().ToArray();
    if (cards.Length == 0)
    {
        log.Error("No ALSA cards found");
        return;
    }

    var chosen = card.HasValue ? cards.FirstOrDefault(c => c.Id == card.Value) : cards[0];
    if (chosen == null)
    {
        log.Error($"Card {card} not found");
        return;
    }

    log.Info($"Using card {chosen.Name} (id={chosen.Id})");


// App configuration types and helpers
class AppConfig
{
    public int? Card { get; set; }
    public int[] ControlsToSweep { get; set; } = Array.Empty<int>();
    public int Steps { get; set; } = 5;
    public int BaselineSeconds { get; set; } = 3;
    public int SignalSeconds { get; set; } = 3;
    public string ResultsFile { get; set; } = "results.json";
    public bool ShowGui { get; set; } = true;
    public int GuiTimeoutSeconds { get; set; } = 60;
}

static AppConfig LoadConfig(string path)
{
    try
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Config file '{path}' not found, using defaults.");
            return new AppConfig();
        }
        var txt = File.ReadAllText(path);
        var cfg = JsonSerializer.Deserialize<AppConfig>(txt, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return cfg ?? new AppConfig();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to load config '{path}': {ex}");
        return new AppConfig();
    }
}

// Results data structure
class SweepResult
{
    public int ControlIndex { get; set; }
    public int ChannelIndex { get; set; }
    public long BestValue { get; set; }
    public double BestNoise { get; set; }
    public double BaselineNoise { get; set; }
    public double BaselineSignal { get; set; }
}

    // enumerate controls
    var controls = chosen.GetMixerControls();
    log.Info($"Found {controls.Length} controls for card '{chosen.Name}'");

    // Print brief table of controls with channels and raw ranges
    Console.WriteLine("Controls:\n");
    for (int i = 0; i < controls.Length; i++)
    {
        var c = controls[i];
        Console.WriteLine($"[{i}] {c.ControlName} ({c.Channels.Length} channels)");
        foreach (var ch in c.Channels)
        {
            Console.WriteLine($"    {ch.Name}: raw={ch.Raw} range=[{ch.Min},{ch.Max}]");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Interactive mode: type the control index to adjust a control, 'measure' to run a measurement, or 'quit' to exit.");
    Console.WriteLine("Additional commands: 'monitor' to continuously show dBFS, 'autosweep' to automatically sweep controls to lower noise floor.");

    while (true)
    {
        Console.Write("> ");
        var line = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(line)) continue;
        if (line.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;
        if (line.Equals("measure", StringComparison.OrdinalIgnoreCase))
        {
            await MeasureLoopback(chosen, controls, log);
            continue;
        }
        if (line.Equals("monitor", StringComparison.OrdinalIgnoreCase))
        {
            await MonitorLoop(chosen, log);
            continue;
        }
        if (line.Equals("autosweep", StringComparison.OrdinalIgnoreCase))
        {
            await AutoSweepInteractive(chosen, controls, log);
            // refresh controls after sweep
            controls = chosen.GetMixerControls();
            continue;
        }

        if (int.TryParse(line, out var idx) && idx >= 0 && idx < controls.Length)
        {
            await AdjustControlInteractive(chosen, controls[idx], log);
            // refresh controls after change
            controls = chosen.GetMixerControls();
            continue;
        }

        Console.WriteLine("Unknown command");
    }
}

async Task MeasureLoopback(Card chosen, MixerControlInfo[] controls, ILog<Program> log)
{
    // Perform a short playback of a test tone and concurrently record the input
    // to estimate dBFS (per-channel RMS). We use AlsaDeviceBuilder to create
    // an ISoundDevice configured to the selected card.
    var settings = new SoundDeviceSettings
    {
        PlaybackDeviceName = $"hw:CARD={chosen.Name}",
        RecordingDeviceName = $"hw:CARD={chosen.Name}",
        MixerDeviceName = $"hw:CARD={chosen.Name}",
        RecordingSampleRate = 48000,
        RecordingChannels = 2,
        RecordingBitsPerSample = 16
    };

    using var device = AlsaDeviceBuilder.Create(settings);

    int durationSeconds = 3;
    int sampleRate = (int)settings.RecordingSampleRate;

    // generate a short 1kHz sine test tone (stereo) at 0.5 amplitude
    var tone = GenerateToneWav(sampleRate, 2, 16, 1000, durationSeconds, 0.5);

    var rmsLeft = new List<double>();
    var rmsRight = new List<double>();

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds + 1));

    bool headerSeen = false;

    void OnData(byte[] buffer)
    {
        // The first callback is the WAV header bytes; skip them.
        if (!headerSeen)
        {
            headerSeen = true;
            return;
        }

        // expecting 16-bit LE stereo interleaved samples
        int bytesPerSample = 2;
        int channels = 2;
        int frameCount = buffer.Length / (bytesPerSample * channels);
        if (frameCount <= 0) return;

        long sumSqL = 0;
        long sumSqR = 0;
        int samples = 0;

        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * channels * bytesPerSample;
            short sL = BitConverter.ToInt16(buffer, offset);
            short sR = BitConverter.ToInt16(buffer, offset + 2);
            sumSqL += (long)sL * sL;
            sumSqR += (long)sR * sR;
            samples++;
        }

        double rmsL = Math.Sqrt(sumSqL / (double)samples) / 32768.0;
        double rmsR = Math.Sqrt(sumSqR / (double)samples) / 32768.0;

        rmsLeft.Add(rmsL);
        rmsRight.Add(rmsR);
    }

    var recordTask = Task.Run(() => device.Record(OnData, cts.Token));

    // play tone (blocks until done)
    device.Play(new MemoryStream(tone), cts.Token);

    // wait for recorder to finish
    try { await recordTask; } catch { }

    if (rmsLeft.Count == 0)
    {
        Console.WriteLine("No recorded data captured");
        return;
    }

    double avgL = rmsLeft.Average();
    double avgR = rmsRight.Average();

    double dbfsL = 20 * Math.Log10(Math.Max(avgL, 1e-12));
    double dbfsR = 20 * Math.Log10(Math.Max(avgR, 1e-12));

    Console.WriteLine($"Measured dBFS Left: {dbfsL:F1} dBFS");
    Console.WriteLine($"Measured dBFS Right: {dbfsR:F1} dBFS");
}

async Task MonitorLoop(Card chosen, ILog<Program> log)
{
    Console.WriteLine("Starting continuous monitor (press Enter to stop)");
    var settings = new SoundDeviceSettings
    {
        PlaybackDeviceName = $"hw:CARD={chosen.Name}",
        RecordingDeviceName = $"hw:CARD={chosen.Name}",
        MixerDeviceName = $"hw:CARD={chosen.Name}",
        RecordingSampleRate = 48000,
        RecordingChannels = 2,
        RecordingBitsPerSample = 16
    };

    using var device = AlsaDeviceBuilder.Create(settings);
    using var cts = new CancellationTokenSource();
    var queue = new System.Collections.Concurrent.ConcurrentQueue<byte[]>();

    void OnData(byte[] buffer)
    {
        // skip WAV header if present
        queue.Enqueue(buffer);
        // limit queue size
        while (queue.Count > 50) queue.TryDequeue(out _);
    }

    var recordTask = Task.Run(() => device.Record(OnData, cts.Token));

    // pump loop to compute RMS per second
    var stopTask = Task.Run(() => Console.ReadLine());
    while (!stopTask.IsCompleted)
    {
        await Task.Delay(1000);
        // gather available buffers
        var list = new List<byte[]>();
        while (queue.TryDequeue(out var buf)) list.Add(buf);
        if (list.Count == 0) continue;

        // compute RMS over collected buffers
        long sumSqL = 0, sumSqR = 0; long samples = 0;
        foreach (var b in list)
        {
            // try skip possible header
            int offset = 0;
            int bytesPerSample = 2; int channels = 2;
            int frameCount = b.Length / (bytesPerSample * channels);
            for (int i = 0; i < frameCount; i++)
            {
                int o = offset + i * channels * bytesPerSample;
                if (o + 3 >= b.Length) break;
                short sL = BitConverter.ToInt16(b, o);
                short sR = BitConverter.ToInt16(b, o + 2);
                sumSqL += (long)sL * sL;
                sumSqR += (long)sR * sR;
                samples++;
            }
        }

        if (samples == 0) continue;
        double rmsL = Math.Sqrt(sumSqL / (double)samples) / 32768.0;
        double rmsR = Math.Sqrt(sumSqR / (double)samples) / 32768.0;
        double dbfsL = 20 * Math.Log10(Math.Max(rmsL, 1e-12));
        double dbfsR = 20 * Math.Log10(Math.Max(rmsR, 1e-12));
        Console.WriteLine($"Monitor dBFS L={dbfsL:F1} dBFS R={dbfsR:F1} dBFS (buffers={list.Count})");
    }

    cts.Cancel();
    try { await recordTask; } catch { }
}

async Task AutoSweepInteractive(Card chosen, MixerControlInfo[] controls, ILog<Program> log)
{
    Console.WriteLine("Enter space-separated control indices to sweep (e.g. '3 5 8'):");
    Console.Write("> ");
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) return;
    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var indices = new List<int>();
    foreach (var p in parts) if (int.TryParse(p, out var v)) indices.Add(v);
    if (indices.Count == 0) return;

    Console.WriteLine("Autosweep will perform multiple short recordings and playbacks. Proceed? (y/n)");
    var ok = Console.ReadLine();
    if (!ok.Equals("y", StringComparison.OrdinalIgnoreCase)) return;

    var probe = new MixerProbe();
    var settings = new SoundDeviceSettings
    {
        PlaybackDeviceName = $"hw:CARD={chosen.Name}",
        RecordingDeviceName = $"hw:CARD={chosen.Name}",
        MixerDeviceName = $"hw:CARD={chosen.Name}",
        RecordingSampleRate = 48000,
        RecordingChannels = 2,
        RecordingBitsPerSample = 16
    };

    using var device = AlsaDeviceBuilder.Create(settings);

    // baseline measurement: noise (no playback) and signal (play tone)
    Console.WriteLine("Measuring baseline noise (3s)");
    double baselineNoise = MeasureNoise(device, 3);
    Console.WriteLine($"Baseline noise RMS: {baselineNoise:E3}");

    Console.WriteLine("Measuring baseline signal (play 3s tone)");
    double baselineSignal = await MeasureSignalAsync(device, 3, 1000);
    Console.WriteLine($"Baseline signal RMS: {baselineSignal:E3}");

    var bestSettings = new Dictionary<int, (int channelIndex, nint value)>();

    foreach (var idx in indices)
    {
        if (idx < 0 || idx >= controls.Length) continue;
        var ctrl = controls[idx];
        Console.WriteLine($"Sweeping control [{idx}] {ctrl.ControlName}");

        for (int ch = 0; ch < ctrl.Channels.Length; ch++)
        {
            var chInfo = ctrl.Channels[ch];
            // coarse sweep: 5 steps across range
            var steps = 5;
            var bestNoise = double.PositiveInfinity;
            nint bestVal = chInfo.Raw;
            for (int s = 0; s <= steps; s++)
            {
                var val = chInfo.Min + ((chInfo.Max - chInfo.Min) * s) / steps;
                nint nval = val;
                // try set capture and playback (whichever succeeds)
                bool set = probe.TrySetCaptureVolume(chosen.Id, ctrl.ControlName, chInfo.Name, nval);
                if (!set) set = probe.TrySetPlaybackVolume(chosen.Id, ctrl.ControlName, chInfo.Name, nval);
                if (!set) continue;

                // small settle time
                await Task.Delay(200);

                var noise = MeasureNoise(device, 3);
                var signal = await MeasureSignalAsync(device, 3, 1000);

                // accept if signal not reduced more than 1dB and noise improved
                double signalDb = 20 * Math.Log10(Math.Max(signal, 1e-12));
                double baselineSignalDb = 20 * Math.Log10(Math.Max(baselineSignal, 1e-12));
                if (signalDb < baselineSignalDb - 1.0) // signal dropped too much
                    continue;

                if (noise < bestNoise)
                {
                    bestNoise = noise;
                    bestVal = nval;
                }
            }

            if (bestNoise < double.PositiveInfinity)
            {
                bestSettings[idx] = (ch, bestVal);
                Console.WriteLine($"Best for control {ctrl.ControlName} channel {chInfo.Name}: value={bestVal} noise={bestNoise:E3}");
                // apply best
                probe.TrySetCaptureVolume(chosen.Id, ctrl.ControlName, chInfo.Name, bestVal);
                probe.TrySetPlaybackVolume(chosen.Id, ctrl.ControlName, chInfo.Name, bestVal);
            }
        }
    }

    Console.WriteLine("Autosweep complete. Applied best found settings.");
}

static double MeasureNoise(ISoundDevice device, int seconds)
{
    var rmsList = new List<double>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 1));
    bool headerSeen = false;
    void OnData(byte[] buffer)
    {
        if (!headerSeen) { headerSeen = true; return; }
        int bytesPerSample = 2; int channels = 2;
        int frameCount = buffer.Length / (bytesPerSample * channels);
        if (frameCount <= 0) return;
        long sumSqL = 0, sumSqR = 0; int samples = 0;
        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * channels * bytesPerSample;
            if (offset + 3 >= buffer.Length) break;
            short sL = BitConverter.ToInt16(buffer, offset);
            short sR = BitConverter.ToInt16(buffer, offset + 2);
            sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
        }
        if (samples == 0) return;
        double rms = Math.Sqrt((sumSqL + sumSqR) / (double)(samples * 2)) / 32768.0;
        rmsList.Add(rms);
    }

    var task = Task.Run(() => device.Record(OnData, cts.Token));
    try { task.Wait(cts.Token); } catch { }
    return rmsList.Count == 0 ? 0.0 : rmsList.Average();
}

static async Task<double> MeasureSignalAsync(ISoundDevice device, int seconds, double freq)
{
    int sampleRate = 48000; int channels = 2; int bits = 16;
    var tone = GenerateToneWav(sampleRate, channels, bits, freq, seconds, 0.5);
    var rmsList = new List<double>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 2));
    bool headerSeen = false;
    void OnData(byte[] buffer)
    {
        if (!headerSeen) { headerSeen = true; return; }
        int bytesPerSample = 2; int chs = 2;
        int frameCount = buffer.Length / (bytesPerSample * chs);
        if (frameCount <= 0) return;
        long sumSqL = 0, sumSqR = 0; int samples = 0;
        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * chs * bytesPerSample;
            if (offset + 3 >= buffer.Length) break;
            short sL = BitConverter.ToInt16(buffer, offset);
            short sR = BitConverter.ToInt16(buffer, offset + 2);
            sumSqL += (long)sL * sL; sumSqR += (long)sR * sR; samples++;
        }
        if (samples == 0) return;
        double rms = Math.Sqrt((sumSqL + sumSqR) / (double)(samples * 2)) / 32768.0;
        rmsList.Add(rms);
    }

    var recordTask = Task.Run(() => device.Record(OnData, cts.Token));
    // play tone
    device.Play(new MemoryStream(tone), cts.Token);
    try { await recordTask; } catch { }
    return rmsList.Count == 0 ? 0.0 : rmsList.Average();
}

static byte[] GenerateToneWav(int sampleRate, int channels, int bitsPerSample, double freq, int seconds, double amplitude)
{
    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);

    int byteRate = sampleRate * channels * bitsPerSample / 8;
    int blockAlign = channels * bitsPerSample / 8;

    // RIFF header
    bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
    bw.Write(36 + sampleRate * seconds * blockAlign); // ChunkSize (approx)
    bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

    // fmt subchunk
    bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
    bw.Write(16); // Subchunk1Size
    bw.Write((short)1); // AudioFormat PCM
    bw.Write((short)channels);
    bw.Write(sampleRate);
    bw.Write(byteRate);
    bw.Write((short)blockAlign);
    bw.Write((short)bitsPerSample);

    // data subchunk header
    bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
    int dataSize = sampleRate * seconds * blockAlign;
    bw.Write(dataSize);

    int totalFrames = sampleRate * seconds;
    for (int i = 0; i < totalFrames; i++)
    {
        double t = i / (double)sampleRate;
        double s = Math.Sin(2.0 * Math.PI * freq * t) * amplitude;
        short sample = (short)(s * short.MaxValue);
        for (int ch = 0; ch < channels; ch++) bw.Write(sample);
    }

    return ms.ToArray();
}

async Task AdjustControlInteractive(Card chosen, MixerControlInfo control, ILog<Program> log)
{
    Console.WriteLine($"Adjusting control: {control.ControlName}");
    if (control.Channels.Length == 0)
    {
        Console.WriteLine("No adjustable channels detected");
        return;
    }

    for (int i = 0; i < control.Channels.Length; i++)
    {
        var ch = control.Channels[i];
        Console.WriteLine($"[{i}] {ch.Name}: raw={ch.Raw} range=[{ch.Min},{ch.Max}]");
    }

    Console.WriteLine("Specify channel index and new raw value (or 'cancel')");
    Console.Write("> ");
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line) || line.Equals("cancel", StringComparison.OrdinalIgnoreCase)) return;
    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length < 2) { Console.WriteLine("Please provide '<channelIndex> <rawValue>'"); return; }
    if (!int.TryParse(parts[0], out var chIdx) || chIdx < 0 || chIdx >= control.Channels.Length) { Console.WriteLine("Invalid channel index"); return; }
    if (!nint.TryParse(parts[1], out var newRaw)) { Console.WriteLine("Invalid raw value"); return; }

    // Attempt to set using the mixer probe API - we don't have a set API on MixerControlInfo, so use lower-level Interop via MixerProbe.
    try
    {
        var probe = new MixerProbe();
        var success = probe.TrySetPlaybackVolume(chosen.Id, control.ControlName, control.Channels[chIdx].Name, newRaw);
        Console.WriteLine(success ? "Set succeeded" : "Set failed");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error adjusting control: {ex}");
    }
}
