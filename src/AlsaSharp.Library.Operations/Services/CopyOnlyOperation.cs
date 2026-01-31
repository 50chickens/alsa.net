using AlsaSharp;
using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Library.Operations.Services;

public class CopyOnlyOperation(ILog<CopyOnlyOperation> logger, ICopyOnlyService copyService) : ICopyOnlyOperation
{
    public async Task<CopyOnlyResponse> ExecuteAsync(CopyOnlyRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.Info($"Executing copy-only operation for {request.DurationMs}ms");
            
            // TODO: Map CardSelector to ISoundDevice - device mapping will be implemented later
            ISoundDevice? device = null; // Placeholder
            
            if (device == null)
            {
                return new CopyOnlyResponse(false, "Device mapping not yet implemented");
            }
            
            // Execute copy operation
            await Task.Run(() => 
                copyService.CopyAudioChannels(device, request.DurationMs, cancellationToken), 
                cancellationToken);
            
            return new CopyOnlyResponse(true, "Copy operation completed successfully");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Copy operation failed");
            return new CopyOnlyResponse(false, ex.Message);
        }
    }
}
