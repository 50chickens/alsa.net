using Microsoft.Extensions.Logging;
using AlsaSharp.Library.Logging;

namespace Example.AlsaSanity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());
            var msLogger = loggerFactory.CreateLogger<AlsaSanityTester>();
            var logger = new LoggerAdapter<AlsaSanityTester>(msLogger);
            var sanityTester = new AlsaSanityTester("default", logger);
            sanityTester.TestSanity();
        }
    }
}
