namespace Example.SNRReduction.Models;

public class AudioMeterLevelReading()
{
    public DateTime TimestampUtc { get; set; }
    public double LeftDbfs { get; set; }
    public double RightDbfs { get; set; }
    public double LeftRms { get; set; }
    public double RightRms { get; set; }
    public override string ToString()
    {
        if (double.IsNaN(RightDbfs))
        {
            return $"LeftDbfs={LeftDbfs:F2} dBFS, LeftRms={LeftRms:F6} (1 channel).";
        }
        else
        {
            return $"LeftDbfs={LeftDbfs:F2} dBFS, RightDbfs={RightDbfs:F2} dBFS, LeftRms={LeftRms:F6}, RightRms={RightRms:F6}. ";
        }
    }
}