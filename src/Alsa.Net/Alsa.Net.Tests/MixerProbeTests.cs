using Alsa.Net.Internal;
using NUnit.Framework;
using System.Linq;

namespace Alsa.Net.Tests
{
    [TestFixture]
    public class MixerProbeTests
    {
        [Test]
        [Category("Integration")]
        public void GetControlsForDefaultCard_ReturnsControls()
        {
            var alsaCardEnumerator = new AlsaCardEnumerator();
            var cards = alsaCardEnumerator.GetCards();
            Assert.IsNotNull(cards.FirstOrDefault(), "No ALSA cards found on system.");

        }
    }
}
