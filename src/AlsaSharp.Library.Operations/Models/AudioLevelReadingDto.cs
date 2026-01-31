namespace AlsaSharp.Library.Operations.Models;

public record AudioLevelReadingDto(
    DateTime Timestamp,
    double[] ChannelLevelsDbfs,
    double[] ChannelPeaks
);
