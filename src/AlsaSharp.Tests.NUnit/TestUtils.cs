using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace AlsaSharp.Tests
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
