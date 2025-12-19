using Example.SNRReduction.Models;

namespace Example.SNRReduction.Services
{
    public class FileNameGenerator
    {
        public string GetFileName(string fileNameDescription)
        {
            var sanitizedDescription = string.Concat(fileNameDescription.Split(Path.GetInvalidFileNameChars()));
            return $"~./SNRREduction/{sanitizedDescription}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        }
    }
}
