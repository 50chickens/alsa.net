namespace Example.SNRReduction.Models;

using System.Collections.Generic;
using System.Linq;

public class AudioMeterLevelReading()
{
    public DateTime TimestampUtc { get; set; }
    // Per-channel readings; index 0..N-1
    public List<double> ChannelDbfs { get; set; } = new List<double>();
    public List<double> ChannelRms { get; set; } = new List<double>();
    public override string ToString()
    {
        if (ChannelDbfs == null || ChannelDbfs.Count == 0) return "No channels";
        // Display RMS in dBFS units (use ChannelDbfs which already contains 20*log10(rms)).
        if (ChannelDbfs.Count == 1) return $"Channel1Dbfs={ChannelDbfs[0]:F2} dBFS, Channel1RmsDbfs={ChannelDbfs[0]:F2} dBFS (1 channel).";
        return string.Join(", ", ChannelDbfs.Select((v, i) => $"Ch{i+1}Dbfs={v:F2}dBFS"));
    }
}