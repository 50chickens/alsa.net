public class AlsaCardInfo
{
    public AlsaCardInfo(int index, string id, string name, string longName, string driver, string mixerName, string components)
    {
        Index = index;
        if (id == null) throw new InvalidOperationException("AlsaCardInfo: id is null");
        Id = id;
        if (name == null) throw new InvalidOperationException("AlsaCardInfo: name is null");
        Name = name;
        if (longName == null) throw new InvalidOperationException("AlsaCardInfo: longName is null");
        LongName = longName;
        if (driver == null) throw new InvalidOperationException("AlsaCardInfo: driver is null");
        Driver = driver;
        if (mixerName == null) throw new InvalidOperationException("AlsaCardInfo: mixerName is null");
        MixerName = mixerName;
        if (components == null) throw new InvalidOperationException("AlsaCardInfo: components is null");
        Components = components;
        PcmEntries = new List<AlsaPcmEntry>();
    }
    public int Index { get; }
    public string Id { get; }
    public string Name { get; }
    public string LongName { get; }
    public string Driver { get; }
    public string MixerName { get; }
    public string Components { get; }
    public List<AlsaPcmEntry> PcmEntries { get; }
}
