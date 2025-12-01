using NUnit.Framework;
using Alsa.Net.Internal;
using System.Linq;

namespace Alsa.Net.Tests
{
    [TestFixture]
    public class CardControlsTests
    {
        [Test]
        [Category("Integration")]
        public void GetControls_ForFirstCard_ReturnsArray()
        {
            var enumerator = new AlsaCardEnumerator();
            var cards = enumerator.GetCards();
            var first = cards.FirstOrDefault();
            Assert.IsNotNull(first, "No ALSA cards found on system to test controls.");

            var controls = first.GetMixerControls();
            Assert.IsNotNull(controls, "GetMixerControls returned null");

            TestContext.Progress.WriteLine($"Found {controls.Length} controls for card '{first.Name}' (id={first.Index})");
        }
    }
}
