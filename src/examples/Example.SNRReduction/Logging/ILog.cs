namespace Example.SNRReduction.Logging;

public interface ILog<T>
{
    void Info(string message);
    void Debug(string message);
    void Warn(string message);
    void Error(Exception ex, string? message = null);
}
