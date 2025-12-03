using NLog;
using NLog.Targets;
using NUnit.Framework;

namespace AlsaSharp.Tests
{
    [Target("NUnitTestContext")]
    public class NUnitTestContextTarget : TargetWithLayout
    {
        protected override void Write(LogEventInfo logEvent)
        {
            var message = this.Layout.Render(logEvent);
            TestContext.Progress.WriteLine(message);
        }
    }
}
