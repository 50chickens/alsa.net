#nullable enable

namespace Example.SNRReduction.Services;

public static class CommandLineParser
{
    public static string? GetArgumentValue(string[] args, string argumentName)
    {
        var prefix = $"--{argumentName}=";
        var arg = args.FirstOrDefault(a => a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        
        return arg?[prefix.Length..];
    }

    public static bool HasArgument(string[] args, string argumentName)
    {
        var flag = $"--{argumentName}";
        return args.Any(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase) || 
                            a.StartsWith($"{flag}=", StringComparison.OrdinalIgnoreCase));
    }
}
