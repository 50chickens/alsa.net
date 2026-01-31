using AlsaSharp;
using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Library.Operations.Services;

public class AudioLevelsOperation(ILog<AudioLevelsOperation> logger, IAudioLevelMeterRecorderService levelMeterService) : IAudioLevelsOperation
{
    public async Task<AudioLevelsResponse> ExecuteAsync(AudioLevelsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.Info($"Executing audio levels measurement for {request.DurationMs}ms");
            
            // TODO: Map CardSelector to ISoundDevice - device mapping will be implemented later
            ISoundDevice? device = null; // Placeholder
            
            if (device == null)
            {
                return new AudioLevelsResponse(false, "Device mapping not yet implemented", null);
            }
            
            // Execute level measurement
            var readings = await Task.Run(() => 
                levelMeterService.RecordAudioMeterLevels(device, cancellationToken), 
                cancellationToken);
            
            // Map results to DTOs
            var readingDtos = readings.Select(r => new AudioLevelReadingDto(
                r.TimestampUtc,
                r.ChannelDbfs.ToArray(),
                r.ChannelRms.ToArray()
            )).ToList();
            
            return new AudioLevelsResponse(true, "Audio level measurement completed successfully", readingDtos);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Audio level measurement failed");
            return new AudioLevelsResponse(false, ex.Message, null);
        }
    }
}
