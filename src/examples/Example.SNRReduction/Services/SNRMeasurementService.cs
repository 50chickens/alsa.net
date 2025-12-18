using System;
using AlsaSharp.Library.Logging;

namespace Example.SNRReduction.Services;

public class SNRMeasurementService(ILog<SNRMeasurementService> log) : ISNRMeasurementService
{
    private const float RANGE_FACTOR = 0.95f;
    private readonly ILog<SNRMeasurementService> _log = log;

    public SNRAnalysisResult AnalyzeSNR(float[] samples, int sampleRate, double targetFreq, int originalFrames = 0, int originalChannels = 1, int originalBytesPerSample = 0)
    {
        if (samples == null)
            throw new ArgumentNullException(nameof(samples));
        if (sampleRate <= 0)
            throw new ArgumentException("sampleRate must be > 0", nameof(sampleRate));
        if (targetFreq <= 0)
            throw new ArgumentException("targetFreq must be > 0", nameof(targetFreq));

        int nsamples = Math.Max(1, (int)Math.Round((double)sampleRate / targetFreq));
        int nsamplesPerSection = nsamples * 2;
        // number of full sections we can examine (each section needs nsamplesPerSection samples)
        int nsection = samples.Length / nsamplesPerSection;

        _log.Trace($"AnalyzeSNR: samples={samples.Length} sampleRate={sampleRate} targetFreq={targetFreq} nsamples={nsamples} nsamplesPerSection={nsamplesPerSection} nsection={nsection}");

        var sectionSnrs = new List<double>();
        double sumSnrPc = 0.0;
        int cntClean = 0, cntNoise = 0;
        // accumulators for average levels and THD
        double sumSignalPower = 0.0; // mean-square of recorded signal (per-section)
        double sumOutputPower = 0.0; // mean-square of output (after gain)
        double sumNoisePower = 0.0;  // mean-square of residual (noise+harmonics)
        double sumThdRatio = 0.0;    // linear THD ratio (noise/harmonic power / fundamental power)

        // generate target mono sine (raw) and compute rms target
        var target = new double[nsamples];
        for (int i = 0; i < nsamples; i++)
        {
            target[i] = Math.Sin(2.0 * Math.PI * i * targetFreq / sampleRate) * RANGE_FACTOR;
        }
        double tmpAcc = 0.0;
        for (int i = 0; i < nsamples; i++)
            tmpAcc += target[i] * target[i];
        double rmsTgt = Math.Sqrt(tmpAcc / nsamples);

        // slide through available sections (overlap permitted)
        for (int s = 0, offset = 0; s < nsection; s++, offset += nsamples)
        {
            double snrDb = double.NaN;

            if (offset + nsamplesPerSection > samples.Length)
            {
                cntNoise++;
                continue;
            }
            // copy section as doubles for numerical stability
            var section = new double[nsamplesPerSection];
            for (int k = 0; k < nsamplesPerSection; k++)
                section[k] = samples[offset + k];

            int shift = -1;
            for (int i = 0; i < nsamples && i + 1 < section.Length; i++)
            {
                if (section[i] >= 0.0 && section[i + 1] < 0.0)
                {
                    double d = section[i] - section[i + 1];
                    if (Math.Abs(d) < 1e-12)
                        continue;
                    shift = i;
                    break;
                }
            }
            if (shift == -1)
            {
                cntNoise++;
                continue;
            }

            // build aligned source of length nsamples using linear interpolation
            var srcAligned = new double[nsamples];
            double a = 0, b = 1;
            {
                double s0 = section[shift];
                double s1 = section[shift + 1];
                double d = s0 - s1;
                if (Math.Abs(d) < 1e-12)
                { a = 1.0; b = 0.0; }
                else
                {
                    a = s0 / d;
                    b = -s1 / d;
                }
            }
            for (int i = 0; i < nsamples; i++)
            {
                int idx = i + shift;
                double v0 = section[idx];
                double v1 = section[idx + 1];
                srcAligned[i] = a * v1 + b * v0;
            }

            // compute rms of aligned source
            double sumSq = 0.0;
            for (int i = 0; i < nsamples; i++)
                sumSq += srcAligned[i] * srcAligned[i];
            double rms = Math.Sqrt(sumSq / nsamples);
            if (rms <= 0)
            {
                cntNoise++;
                continue;
            }

            double gain = rmsTgt / rms;
            for (int i = 0; i < nsamples; i++)
                srcAligned[i] = (srcAligned[i] * gain);

            // compute residual energy
            double residual = 0.0;
            for (int i = 0; i < nsamples; i++)
            {
                double d = target[i] - srcAligned[i];
                residual += d * d;
            }

            double powerRatio = double.NaN;

            if (residual <= 0.0)
            {
                // essentially infinite SNR, clamp to large value
                snrDb = 200.0;
            }
            else
            {
                // compute power ratio: (rmsTgt^2) / (residual/nsamples)
                powerRatio = (rmsTgt * rmsTgt) / (residual / nsamples);
                snrDb = 10.0 * Math.Log10(powerRatio);
            }

            if (!double.IsFinite(snrDb))
            {
                _log.Trace($"Non-finite SNR at section {s}: offset={offset} shift={shift} rmsTgt={rmsTgt:F6} rms={rms:F6} residual={residual:E} powerRatio={powerRatio:E}");
                cntNoise++;
                continue;
            }

            // Per-section numeric trace for diagnostics
            _log.Trace($"Section {s}: offset={offset} shift={shift} rmsTgt={rmsTgt:F6} rms={rms:F6} residual={residual:E} powerRatio={powerRatio:E} snrDb={snrDb:F6}");

            sectionSnrs.Add(snrDb);
            // accumulate linear power (from dB) for averaging across all valid sections
            cntClean++;
            double linearPower = Math.Pow(10.0, snrDb / 10.0);
            sumSnrPc += linearPower;

            // accumulate signal/output/noise powers for level reporting
            double signalPower = rms * rms; // mean-square of recorded aligned source before gain
            double outputRms = rms * gain; // after gain (should be near rmsTgt)
            double outputPower = outputRms * outputRms;
            double noisePower = residual / nsamples;
            // compute THD using Goertzel to estimate harmonic energy (alsabat-style)
            double thdRatio = double.NaN;
            try
            {
                int maxHarmonic = 5; // harmonics 2..5
                double fundamentalPower = GoertzelPower(srcAligned, sampleRate, targetFreq);
                double harmonicPower = 0.0;
                for (int h = 2; h <= maxHarmonic; h++)
                {
                    double f = targetFreq * h;
                    if (f >= sampleRate / 2.0)
                        break; // beyond Nyquist
                    harmonicPower += GoertzelPower(srcAligned, sampleRate, f);
                }
                if (fundamentalPower > 0.0)
                {
                    thdRatio = harmonicPower / fundamentalPower;
                }
            }
            catch { thdRatio = double.NaN; }

            sumSignalPower += signalPower;
            sumOutputPower += outputPower;
            sumNoisePower += noisePower;
            if (double.IsFinite(thdRatio))
                sumThdRatio += thdRatio;
        }

        if (cntClean == 0)
        {
            return new SNRAnalysisResult
            {
                AverageSnrDb = double.NaN,
                CleanSections = cntClean,
                NoiseSections = cntNoise,
                SectionSnrDb = sectionSnrs,
                Frames = originalFrames == 0 ? samples.Length : originalFrames,
                Channels = originalChannels,
                BytesPerSample = originalBytesPerSample,
                SampleRate = sampleRate,
                AverageSignalDb = double.NaN,
                AverageNoiseDb = double.NaN,
                AverageOutputDb = double.NaN,
                TotalHarmonicDistortionDb = double.NaN,
                AverageSnrUnit = "decibels (dB)",
                SectionSnrUnit = "decibels (dB)",
                FramesUnit = "frames",
                ChannelsUnit = "count",
                BytesPerSampleUnit = "bytes",
                SampleRateUnit = "hertz (Hz)",
                AverageSignalUnit = "decibels (dB)",
                AverageNoiseUnit = "decibels (dB)",
                AverageOutputUnit = "decibels (dB)",
                TotalHarmonicDistortionUnit = "decibels (dB)"
            };
        }

        double avgLinearPower = sumSnrPc / cntClean;
        double avgSnrDb = 10.0 * Math.Log10(avgLinearPower);

        // compute average levels (power-averaged then to dB)
        double avgSignalPower = sumSignalPower / cntClean;
        double avgOutputPower = sumOutputPower / cntClean;
        double avgNoisePower = sumNoisePower / cntClean;

        double avgSignalDb = double.IsNaN(avgSignalPower) || avgSignalPower <= 0.0 ? double.NaN : 10.0 * Math.Log10(avgSignalPower);
        double avgOutputDb = double.IsNaN(avgOutputPower) || avgOutputPower <= 0.0 ? double.NaN : 10.0 * Math.Log10(avgOutputPower);
        double avgNoiseDb = double.IsNaN(avgNoisePower) || avgNoisePower <= 0.0 ? double.NaN : 10.0 * Math.Log10(avgNoisePower);

        double avgThdDb = double.NaN;
        if (sumThdRatio > 0.0)
        {
            double avgThdRatio = sumThdRatio / cntClean;
            if (avgThdRatio > 0.0)
                avgThdDb = 10.0 * Math.Log10(avgThdRatio);
        }

        return new SNRAnalysisResult
        {
            AverageSnrDb = avgSnrDb,
            CleanSections = cntClean,
            NoiseSections = cntNoise,
            SectionSnrDb = sectionSnrs,
            Frames = originalFrames == 0 ? samples.Length : originalFrames,
            Channels = originalChannels,
            BytesPerSample = originalBytesPerSample,
            SampleRate = sampleRate,
            AverageSignalDb = avgSignalDb,
            AverageNoiseDb = avgNoiseDb,
            AverageOutputDb = avgOutputDb,
            TotalHarmonicDistortionDb = avgThdDb
            ,
            AverageSnrUnit = "decibels (dB)",
            SectionSnrUnit = "decibels (dB)",
            FramesUnit = "frames",
            ChannelsUnit = "count",
            BytesPerSampleUnit = "bytes",
            SampleRateUnit = "hertz (Hz)",
            AverageSignalUnit = "decibels (dB)",
            AverageNoiseUnit = "decibels (dB)",
            AverageOutputUnit = "decibels (dB)",
            TotalHarmonicDistortionUnit = "decibels (dB)"
        };
    }

    // Local helper: Goertzel power estimate (mean-square) for a frequency in the provided samples.
    static double GoertzelPower(double[] data, int sampleRate, double freq)
    {
        if (data == null || data.Length == 0)
            return 0.0;
        int N = data.Length;
        double omega = 2.0 * Math.PI * freq / sampleRate;
        double coeff = 2.0 * Math.Cos(omega);
        double s_prev = 0.0, s_prev2 = 0.0;
        for (int i = 0; i < N; i++)
        {
            double s = data[i] + coeff * s_prev - s_prev2;
            s_prev2 = s_prev;
            s_prev = s;
        }
        // magnitude squared (unnormalized)
        double power = s_prev * s_prev + s_prev2 * s_prev2 - coeff * s_prev * s_prev2;
        if (!double.IsFinite(power) || power < 0.0)
            power = 0.0;
        return power / N;
    }
}
