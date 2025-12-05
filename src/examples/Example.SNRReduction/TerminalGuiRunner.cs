using AlsaSharp;
using Examples.SNRReduction.Models;
using AlsaSharp.Library.Logging;
using Example.SNRReduction.Audio;
using System.Threading;
using Examples.SNRReduction.Services;

namespace Example.SNRReduction;

public class TerminalGuiRunner(ILog<TerminalGuiRunner> log, AudioInterfaceLevelMeter levelMeter)
{
    private readonly ILog<TerminalGuiRunner> _log = log;
    private readonly AudioInterfaceLevelMeter _levelMeter = levelMeter;

    // public async Task Run(SNRReductionServiceOptions options)
    // {
    //     var soundSettings = new SoundDeviceSettings();
    //     if (!string.IsNullOrEmpty(options.AudioCardName))
    //     {
    //         soundSettings.RecordingDeviceName = options.AudioCardName;
    //         soundSettings.MixerDeviceName = options.AudioCardName;
    //         soundSettings.PlaybackDeviceName = options.AudioCardName;
    //     }

    // }

    // private void DrawMeters(double leftDbfs, double rightDbfs)
    // {
    //     // Map dBFS (-120..0) to 0..50 characters. Clamp non-finite values to -120 dBFS
    //     int width = 50;
    //     double l = double.IsFinite(leftDbfs) ? leftDbfs : -120.0;
    //     double r = double.IsFinite(rightDbfs) ? rightDbfs : -120.0;
    //     l = Math.Clamp(l, -120.0, 0.0);
    //     r = Math.Clamp(r, -120.0, 0.0);

    //     int leftBars = (int)Math.Round((l + 120.0) / 120.0 * width);
    //     int rightBars = (int)Math.Round((r + 120.0) / 120.0 * width);

    //     Console.SetCursorPosition(0, 2);
    //     Console.WriteLine($"Left : [{new string('#', leftBars)}{new string(' ', width - leftBars)}] {l,6:F2} dBFS   ");
    //     Console.WriteLine($"Right: [{new string('#', rightBars)}{new string(' ', width - rightBars)}] {r,6:F2} dBFS   ");
    // }

    // private bool ValidateRecordingDevice(ISoundDevice device, out string message)
    // {
    //     message = string.Empty;
    //     if (device == null) { message = "device is null"; return false; }

    //     bool dataSeen = false;
    //     try
    //     {
    //         using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    //         void OnData(byte[] buffer)
    //         {
    //             dataSeen = true;
    //         }

    //         var task = System.Threading.Tasks.Task.Run(() => device.Record(OnData, cts.Token));
    //         try { task.Wait(cts.Token); } catch (OperationCanceledException) { }
    //         catch (AggregateException ae)
    //         {
    //             message = "record task aggregate exception: " + ae.InnerException?.Message;
    //             return false;
    //         }
    //         catch (Exception ex)
    //         {
    //             message = "record task failed: " + ex.Message;
    //             return false;
    //         }

    //         if (!dataSeen)
    //         {
    //             message = "no audio frames received from device within timeout";
    //             return false;
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         message = ex.Message;
    //         return false;
    //     }

    //     return true;
    // }
}
