using NUnit.Framework;
using System.IO;
using Alsa.Net.Internal;

namespace Alsa.Net.Tests
{
    [TestFixture]
    public class MixerProbeTests
    {
        [Test]
        [Category("Integration")]
        public void GetControlsForDefaultCard_ReturnsControls()
        {
            var cards = AlsaCardEnumerator.GetCards();
            Assert.IsNotNull(cards.FirstOrDefault(), "No ALSA cards found on system.");
              
        }
    }
}
