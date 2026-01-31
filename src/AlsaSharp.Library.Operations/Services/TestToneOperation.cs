using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Operations.Models;
using AlsaSharp.Library.Services;

namespace AlsaSharp.Library.Operations.Services;

public class TestToneOperation(ILog<TestToneOperation> logger, ITestToneService testToneService) : ITestToneOperation
{
    public async Task<TestToneResponse> ExecuteAsync(TestToneRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.Info($"Executing test tone: {request.FrequencyHz}Hz at {request.AmplitudeDbfs}dBFS for {request.DurationMs}ms");
            
            // TODO: Map CardSelector to device name - device mapping will be implemented later
            string? deviceName = null; // Placeholder
            
            if (deviceName == null)
            {
                return new TestToneResponse(false, "Device mapping not yet implemented");
            }
            
            // Execute test tone playback
            // NOTE: TestToneService plays tones on left, right, and both channels sequentially
            // Currently using the same duration for all three phases
            // TODO: Consider updating TestToneRequest to include separate durations for each channel
            await Task.Run(() => 
                testToneService.PlayTestTone(
                    deviceName, 
                    request.FrequencyHz, 
                    request.AmplitudeDbfs, 
                    request.DurationMs,  // Left channel duration
                    request.DurationMs,  // Right channel duration
                    request.DurationMs   // Both channels duration
                ), 
                cancellationToken);
            
            return new TestToneResponse(true, "Test tone played successfully");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Test tone playback failed");
            return new TestToneResponse(false, ex.Message);
        }
    }
}
