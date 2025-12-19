using Microsoft.Extensions.Logging;

namespace Example.AlsaSanity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());
            var logger = loggerFactory.CreateLogger<AlsaSanityTester>();
            var sanityTester = new AlsaSanityTester("default", logger);
            sanityTester.TestSanity();
        }
    }
}
