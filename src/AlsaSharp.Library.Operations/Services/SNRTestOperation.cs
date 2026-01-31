using AlsaSharp;
using AlsaSharp.Library.Logging;
using AlsaSharp.Library.Operations.Models;

namespace AlsaSharp.Library.Operations.Services;

public class SNRTestOperation(ILog<SNRTestOperation> logger, ISNRMeasurementService snrService, IAudioRecorderService recorderService) : ISNRTestOperation
{
    public async Task<SNRTestResponse> ExecuteAsync(SNRTestRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.Info($"Executing SNR test for frequency {request.TargetFrequencyHz}Hz");
            
            // TODO: Map CardSelector to ISoundDevice - device mapping will be implemented later
            ISoundDevice? device = null; // Placeholder
            
            if (device == null)
            {
                return new SNRTestResponse(false, "Device mapping not yet implemented", null);
            }
            
            // Execute SNR measurement
            await Task.Run(() => snrService.MeasureSNR(device, request.TargetFrequencyHz, cancellationToken), cancellationToken);
            
            // Get recorded samples for analysis
            var samples = await Task.Run(() => recorderService.RecordToFloatArray(device, 10000, cancellationToken), cancellationToken);
            
            // Analyze SNR
            var result = snrService.MeasureSNRforAudioDevice(samples, device, request.TargetFrequencyHz);
            
            // Map result to DTO
            var resultDto = new SNRTestResultDto(
                result.AverageSnrDb,
                request.TargetFrequencyHz,
                result.AverageSignalDb,
                result.AverageNoiseDb
            );
            
            return new SNRTestResponse(true, "SNR test completed successfully", resultDto);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "SNR test failed");
            return new SNRTestResponse(false, ex.Message, null);
        }
    }
}
