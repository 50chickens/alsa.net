using NLog.Targets;
namespace Alsa.Net.Tests.NUnit
{
    public class NUnitLogTarget : TargetWithLayout
    {
        public NUnitLogTarget()
        {
            Layout = "${longdate} | ${logger:shortname=true} - ${message} ${exception:format=ToString}";
            Name = "Nunit";
        }
        protected override void Write(NLog.LogEventInfo logEvent)
        {
            // render the layout and write to stdout so test runners (and NUnit) capture the output
            var text = this.Layout.Render(logEvent);
            System.Console.WriteLine(text);
        }
    }
}
