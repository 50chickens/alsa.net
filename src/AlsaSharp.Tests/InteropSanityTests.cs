using System;
using NUnit.Framework;
using AlsaSharp.Library.Logging;
using AlsaSharp.Tests.NUnit;
using AlsaSharp.Tests.Library;
using AlsaSharp.Core.Native;

namespace AlsaSharp.Tests
{
    [TestFixture]
    public class InteropSanityTests
    {
        private readonly Microsoft.Extensions.Configuration.IConfiguration _iconfiguration = TestUtils.BuildTestConfiguration();
        private ILog<InteropSanityTests> _log;

        [OneTimeSetUp]
        public void Setup()
        {
            var logBuilder = new LogBuilder(_iconfiguration).UseNunitTestContext();
            logBuilder.Build();
            _log = LogManager.GetLogger<InteropSanityTests>();
            _log.Info($"Logger initialized for {GetType().Name}.");
        }

        [Test]
        [Explicit]
        [Category("InteropSanity")]
        public void RunBasicAlsaProbes()
        {
            _log?.Info("Starting interop sanity probes...");

            // snd_card_next
            int card = -1;
            var nextRes = InteropDiagnostics.CardNext(ref card);
            if (!nextRes.Success)
            {
                TestContext.Progress.WriteLine($"snd_card_next threw: {nextRes.Error}");
                Assert.Inconclusive("snd_card_next threw an exception");
                return;
            }

            _log?.Info($"snd_card_next -> rc={nextRes.Result}, card={card}");

            if (card >= 0)
            {
                var nameRes = InteropDiagnostics.CardGetName(card);

                if (!nameRes.Success)
                {
                    TestContext.Progress.WriteLine($"snd_card_get_name threw: {nameRes.Error}");
                }
                else
                {
                    var (returnCode, pointer) = nameRes.Result;
                    _log?.Info($"snd_card_get_name -> rc={returnCode}, ptr=0x{pointer.ToInt64():x}");

                    var str = NativeDiagnostics.PtrToStringUtf8Safe(pointer, "snd_card_get_name");
                    _log?.Info($"Marshal result success={str.Success}, value='{str.Result}', error={str.Error}");

                    if (pointer != IntPtr.Zero)
                    {
                        InteropDiagnostics.Free(pointer);
                    }
                }
            }

            // Mixer open/attach/register/load
            IntPtr mixer = IntPtr.Zero;
            var openRes = InteropDiagnostics.MixerOpen(out mixer);
            _log?.Info($"snd_mixer_open -> success={openRes.Success}, rc={openRes.Result}, error={openRes.Error}");

            if (openRes.Success && openRes.Result == 0 && mixer != IntPtr.Zero)
            {
                try
                {
                    var attachRes = InteropDiagnostics.MixerAttach(mixer, card >= 0 ? $"hw:{card}" : "default");
                    _log?.Info($"snd_mixer_attach -> success={attachRes.Success}, rc={attachRes.Result}, error={attachRes.Error}");

                    var regRes = InteropDiagnostics.MixerSelemRegister(mixer);
                    _log?.Info($"snd_mixer_selem_register -> success={regRes.Success}, rc={regRes.Result}, error={regRes.Error}");

                    var loadRes = InteropDiagnostics.MixerLoad(mixer);
                    _log?.Info($"snd_mixer_load -> success={loadRes.Success}, rc={loadRes.Result}, error={loadRes.Error}");

                    if (loadRes.Success && loadRes.Result == 0)
                    {
                        var firstElem = InteropDiagnostics.MixerFirstElem(mixer);
                        _log?.Info($"snd_mixer_first_elem -> success={firstElem.Success}, ptr=0x{(firstElem.Result == IntPtr.Zero ? 0 : firstElem.Result.ToInt64()):x}, error={firstElem.Error}");
                    }
                }
                finally
                {
                    var closeRes = InteropDiagnostics.MixerClose(mixer);
                    if (!closeRes.Success || closeRes.Result < 0)
                    {
                        throw new InvalidOperationException($"snd_mixer_close failed: {(closeRes.Error ?? ("errno " + closeRes.Result))}");
                    }
                }
            }

            _log?.Info("Interop sanity probes completed.");
        }
    }
}
