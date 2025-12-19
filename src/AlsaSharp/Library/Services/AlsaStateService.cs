using AlsaSharp.Library.Logging;
using Microsoft.Extensions.Logging;

public class AlsaStateService(ILogger<AlsaStateService> log)
{
    private readonly ILogger<AlsaStateService> _log = log;

    /// <summary>
    /// Restore ALSA mixer element states from an ALSA state file.
    /// </summary>
    /// <param name="stateFilePath">Path to the ALSA state file.</param>
    /// 
public void RestoreStateFromAlsaStateFile(string stateFilePath)
    {
        if (string.IsNullOrEmpty(stateFilePath) || !File.Exists(stateFilePath))
        {
            _log?.LogWarning("[ALSA INFO] State file not found: {Path}", stateFilePath);
            return;
        }

        string[] lines = File.ReadAllLines(stateFilePath);
        string? currentName = null;
        // simple parser: when we find a name '...' line, collect subsequent value lines until the next control or closing brace
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim(); //replace line with a regex to remove leading/trailing whitespace
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;
            if (line.StartsWith("name "))
            {
                // name 'Mic 1 Volume'
                int firstQuote = line.IndexOf('\'');
                int lastQuote = line.LastIndexOf('\'');
                if (firstQuote >= 0 && lastQuote > firstQuote)
                {
                    currentName = line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                }
                else
                {
                    // fallback: split
                    var parts = line.Split(new[] { ' ' }, 2);
                    currentName = parts.Length > 1 ? parts[1].Trim().Trim('"') : null;
                }
            }

            if (currentName != null && line.StartsWith("value"))
            {
                // value, value.0, value.1 formats
                // examples: value 0  OR value.0 53  OR value false
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string key = parts[0];
                    string rawVal = parts[1];
                    // if more tokens exist, last token is value
                    if (parts.Length > 2)
                        rawVal = parts[parts.Length - 1];

                    // try boolean
                    if (string.Equals(rawVal, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(rawVal, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        int v = string.Equals(rawVal, "true", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                        // channel specified?
                        if (key.Contains('.'))
                        {
                            var idxPart = key.Split('.').Last();
                            if (int.TryParse(idxPart, out int idx) && idx >= 0)
                            {
                                var ch = idx == 0 ? "left" : (idx == 1 ? "right" : "");
                                try
                                { SetSimpleElementValue(currentName, ch, v); }
                                catch (Exception ex) { _log?.LogError(ex, "[ALSA ERROR] RestoreState: failed to set {Name}:{Channel} -> {Value}", currentName, ch, v); }
                            }
                        }
                        else
                        {
                            try
                            { SetSimpleElementValue(currentName, string.Empty, v); }
                            catch (Exception ex) { _log?.LogError(ex, "[ALSA ERROR] RestoreState: failed to set {Name} -> {Value}", currentName, v); }
                        }
                        continue;
                    }

                    // try integer
                    if (int.TryParse(rawVal, out int intVal))
                    {
                        if (key.Contains('.'))
                        {
                            var idxPart = key.Split('.').Last();
                            if (int.TryParse(idxPart, out int idx) && idx >= 0)
                            {
                                var ch = idx == 0 ? "left" : (idx == 1 ? "right" : "");
                                try
                                { SetSimpleElementValue(currentName, ch, intVal); }
                                catch (Exception ex) { _log?.LogError(ex, "[ALSA ERROR] RestoreState: failed to set {Name}:{Channel} -> {Value}", currentName, ch, intVal); }
                            }
                        }
                        else
                        {
                            try
                            { SetSimpleElementValue(currentName, string.Empty, intVal); }
                            catch (Exception ex) { _log?.LogError(ex, "[ALSA ERROR] RestoreState: failed to set {Name} -> {Value}", currentName, intVal); }
                        }
                        continue;
                    }

                    // could not parse (enum or string) - skip
                    // try to map enumerated item name to its numeric index using MixerService
                    try
                    {
                        var enumCandidate = rawVal.Trim().Trim('\'');
                        var mixerSvc = new AlsaSharp.Library.Services.MixerService();
                        int cardIndex = Settings?.CardIndex ?? 0;
                        if (mixerSvc.TryGetEnumItemIndex(cardIndex, currentName, enumCandidate, out int enumIndex))
                        {
                            if (key.Contains('.'))
                            {
                                var idxPart = key.Split('.').Last();
                                if (int.TryParse(idxPart, out int idx) && idx >= 0)
                                {
                                    var ch = idx == 0 ? "left" : (idx == 1 ? "right" : "");
                                    try
                                    { SetSimpleElementValue(currentName, ch, enumIndex); }
                                    catch (Exception ex) { _log?.LogError(ex, "[ALSA ERROR] RestoreState(enum): failed to set {Name}:{Channel} -> {Value}", currentName, ch, enumIndex); }
                                }
                            }
                            else
                            {
                                try
                                { 
                                    SetSimpleElementValue(currentName, string.Empty, enumIndex); 
                                }
                                catch (Exception ex) 
                                { 
                                    _log?.LogError(ex, "[ALSA ERROR] RestoreState(enum): failed to set {Name} -> {Value}", currentName, enumIndex); 
                                }
                            }
                        }
                        else
                        {
                            _log?.LogInformation("[ALSA INFO] RestoreState: skipping unsupported value for control '{Name}': {Raw}", currentName, rawVal);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log?.LogInformation("[ALSA INFO] RestoreState: skipping unsupported value for control '{Name}': {Raw} ({Err})", currentName, rawVal, ex.Message);
                    }
                }
            }

            // reset on end of control block
            if (line == "}")
            {
                currentName = null;
            }
        }
    }
}