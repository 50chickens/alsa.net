#nullable enable

using System.Text.RegularExpressions;
using AlsaSharp;
using AlsaSharp.Library.Logging;

namespace Example.SNRReduction.Services;

public class AudioCardSelector : IAudioCardSelector
{
    private readonly ILog<AudioCardSelector> _log;

    public AudioCardSelector(ILog<AudioCardSelector> log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public IEnumerable<ISoundDevice> SelectCards(IEnumerable<ISoundDevice> allDevices, string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            _log.Debug("No card selector specified, returning all devices");
            return allDevices;
        }

        var devices = allDevices.ToList();
        var selected = new List<ISoundDevice>();

        var parts = selector.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (int.TryParse(part, out int cardIndex))
            {
                var device = SelectByIndex(devices, cardIndex);
                if (device != null)
                {
                    selected.Add(device);
                    _log.Debug($"Selected card by index {cardIndex}: {device.Settings.CardName}");
                }
                else
                {
                    _log.Warn($"Card index {cardIndex} not found");
                }
            }
            else
            {
                var matchedDevices = SelectByNamePattern(devices, part);
                selected.AddRange(matchedDevices);
                
                if (matchedDevices.Any())
                {
                    _log.Debug($"Selected {matchedDevices.Count()} card(s) matching pattern '{part}': {string.Join(", ", matchedDevices.Select(d => d.Settings.CardName))}");
                }
                else
                {
                    _log.Warn($"No cards found matching pattern '{part}'");
                }
            }
        }

        return selected.Distinct();
    }

    private ISoundDevice? SelectByIndex(List<ISoundDevice> devices, int index)
    {
        return index >= 0 && index < devices.Count ? devices[index] : null;
    }

    private IEnumerable<ISoundDevice> SelectByNamePattern(List<ISoundDevice> devices, string pattern)
    {
        try
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            
            return devices.Where(device =>
            {
                var longName = device.Settings.CardLongName ?? string.Empty;
                var shortName = device.Settings.CardName ?? string.Empty;
                
                return regex.IsMatch(longName) || regex.IsMatch(shortName);
            });
        }
        catch (ArgumentException ex)
        {
            _log.Error($"Invalid regex pattern '{pattern}': {ex.Message}");
            return Enumerable.Empty<ISoundDevice>();
        }
    }
}
