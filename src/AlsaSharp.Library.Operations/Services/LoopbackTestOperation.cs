using AlsaSharp;
using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Library.Operations.Services;

public class LoopbackTestOperation(ILog<LoopbackTestOperation> logger, IAlsaLoopbackTestService loopbackService) : ILoopbackTestOperation
{
    public async Task<LoopbackTestResponse> ExecuteAsync(LoopbackTestRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.Info("Executing loopback test");
            
            // TODO: Map CardSelector to ISoundDevice - device mapping will be implemented later
            // For now, this is a placeholder that would need actual device resolution
            ISoundDevice? device = null; // Placeholder
            
            if (device == null)
            {
                return new LoopbackTestResponse(false, "Device mapping not yet implemented", null);
            }
            
            // Execute the loopback test
            var result = await Task.Run(() => 
                loopbackService.TestLoopback(device, device, request.TestDurationMs, request.TestLevelDbfs), 
                cancellationToken);
            
            // Map result to DTO
            var resultDto = new LoopbackTestResultDto(
                result.RecordedSignal.Samples,
                result.RecordedSignal.ChannelDbfs,
                result.RecordedSignal.ChannelRms,
                result.RecordedSignal.PeakAmplitude,
                result.RecordedSignal.SignalToNoiseRatio,
                result.RecordedSignal.TotalHarmonicDistortionDb,
                result.RecordedSignal.HasSignal,
                result.RecordedSignal.RoundTripLatencyMs,
                result.RecordedSignal.LatencySamples,
                result.RecordedSignal.ConfidenceScore,
                result.RecordedSignal.LatencyMeasurementValid
            );
            
            return new LoopbackTestResponse(true, "Loopback test completed successfully", resultDto);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Loopback test failed");
            return new LoopbackTestResponse(false, ex.Message, null);
        }
    }
}
