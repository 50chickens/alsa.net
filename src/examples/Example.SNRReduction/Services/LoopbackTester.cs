using AlsaSharp;
using AlsaSharp.Library.Logging;

namespace Example.SNRReduction.Services;

public class LoopbackTester(ILog<LoopbackTester> log, ISNRWorkerHelper helper) : ILoopbackTester
{
    private readonly ILog<LoopbackTester> _log = log;
    private readonly ISNRWorkerHelper _helper = helper;

    public async Task RunLoopbackTestAsync(ISoundDevice device, CancellationToken token)
    {
        if (device == null) return;
        var settings = device.Settings;
        if (settings == null) return;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(TimeSpan.FromSeconds(4));

        var acc = _helper.CreateAccumulator(device);
        using var playStream = _helper.BuildInlineSineWav(settings, 1000.0, 3.0);

        var playTask = Task.Run(() => {
            try { device.Play(playStream, cts.Token); }
            catch (Exception ex) { _log?.Warn($"Loopback tone playback failed: {ex.Message}"); }
        });

        try
        {
            device.Record(acc.OnData, cts.Token);
        }
        catch (Exception)
        {
            // expected on cancel
        }

        var results = acc.ComputeResults();
        _log?.Info($"Loopback test results: {string.Join(", ", results.ChannelDbfs.Select(d => d.ToString("F2") + " dBFS"))}");
        await Task.CompletedTask;
    }
}
