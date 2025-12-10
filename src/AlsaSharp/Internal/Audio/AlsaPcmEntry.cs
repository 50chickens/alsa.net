public class AlsaPcmEntry
{
    public AlsaPcmEntry(int deviceIndex, string id, string name, List<AlsaSubdevice> subdevices, int subdevicesCount, string stream)
    {
        DeviceIndex = deviceIndex;
        if (id == null) throw new InvalidOperationException("AlsaPcmEntry: id is null");
        Id = id;
        if (name == null) throw new InvalidOperationException("AlsaPcmEntry: name is null");
        Name = name;
        if (subdevices == null) throw new InvalidOperationException("AlsaPcmEntry: subdevices is null");
        Subdevices = subdevices;
        if (subdevices.Count == 0) throw new InvalidOperationException("AlsaPcmEntry: expected at least one subdevice");
        SubdeviceName = subdevices[0].Name;
        SubdevicesCount = subdevicesCount;
        if (stream == null) throw new InvalidOperationException("AlsaPcmEntry: stream is null");
        Stream = stream;
    }

    public int DeviceIndex { get; }
    public string Id { get; }
    public string Name { get; }
    public List<AlsaSubdevice> Subdevices { get; }
    public string SubdeviceName { get; }
    public int SubdevicesCount { get; }
    public string Stream { get; }
}
