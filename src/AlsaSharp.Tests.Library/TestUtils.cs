using Microsoft.Extensions.Configuration;


namespace AlsaSharp.Tests.Library
{
    public static class TestUtils
    {
        public static IConfiguration BuildTestConfiguration()
        {
            {
                var values = new Dictionary<string, string?>
            {
                {"Logging:LogLevel", "Debug"},
                {"Logging:EnableConsoleLogging", "true"},
                {"Logging:EnableFileLogging", "false"},
            };
                var configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(values);
                return configurationBuilder.Build();
            }

        }
    }
}
