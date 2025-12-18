namespace AlsaSharp.Library.Extensions;

public static class JsonExtensions
{
    public static string ToJson(this object obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
