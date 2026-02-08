using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Mill.Services;

/// <summary>
/// Shared JSON serialization using AOT-compatible source generators.
/// Options inherit type resolvers from MillJsonContext, so all registered types
/// are serialized via source-generated code — no runtime reflection.
/// </summary>
public static class JsonHelper
{
    public static readonly JsonSerializerOptions Options = new(MillJsonContext.Default.Options)
    {
        WriteIndented = false
    };

    public static readonly JsonSerializerOptions OptionsIndented = new(MillJsonContext.Default.Options)
    {
        WriteIndented = true
    };

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "All types registered in MillJsonContext")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "All types registered in MillJsonContext")]
    public static string Serialize<T>(T value, bool indented = false) =>
        JsonSerializer.Serialize(value, indented ? OptionsIndented : Options);

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "All types registered in MillJsonContext")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "All types registered in MillJsonContext")]
    public static T? Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options);
}
