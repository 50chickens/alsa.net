namespace Example.SNRReduction.Models;

public class AudioMeterLevelReading
{
    public DateTime TimestampUtc { get; set; }
    public double LeftDbfs { get; set; }
    public double RightDbfs { get; set; }
    public double LeftRms { get; set; }
    public double RightRms { get; set; }
}