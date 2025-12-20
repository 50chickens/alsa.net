using Example.SNRReduction.Models;

namespace Example.SNRReduction.Services
{
    public class FileNameGenerator
    {
        public string GetFileName(string fileNameDescription)
        {
            var sanitizedDescription = string.Concat(fileNameDescription.Split(Path.GetInvalidFileNameChars()));
            var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".SNRReduction");
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            
            return Path.Combine(outputDir, $"{sanitizedDescription}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
        }
    }
}
