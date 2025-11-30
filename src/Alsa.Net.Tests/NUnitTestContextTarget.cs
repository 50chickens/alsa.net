using System;
using NLog;
using NLog.Targets;
using NUnit.Framework;

namespace Alsa.Net.Tests
{
    [Target("NUnitTestContext")]
    public class NUnitTestContextTarget : TargetWithLayout
    {
        protected override void Write(LogEventInfo logEvent)
        {
            var message = this.Layout.Render(logEvent);
            // Write to NUnit progress stream so it appears in test output
            try
            {
                TestContext.Progress.WriteLine(message);
            }
            catch
            {
                // ignore if unavailable
            }

            // Also write to Console so Test Explorer Standard Output shows the log
            try
            {
                Console.WriteLine(message);
            }
            catch
            {
            }
        }
    }
}
