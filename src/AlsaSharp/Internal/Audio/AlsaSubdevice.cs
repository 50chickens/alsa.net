public class AlsaSubdevice
{
    public AlsaSubdevice(int subdeviceIndex, string name)
    {
        SubdeviceIndex = subdeviceIndex;
        if (name == null) throw new InvalidOperationException("AlsaSubdevice: name is null");
        Name = name;
    }

    public int SubdeviceIndex { get; }
    public string Name { get; }
}
