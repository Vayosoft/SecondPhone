using System.Text.Json.Serialization;

namespace EmulatorRC.Entities;

public class DeviceSession
{
    public string DeviceId { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    public string StreamType { get; set; } = string.Empty;
}

[JsonSerializable(typeof(DeviceSession))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
public partial class DeviceSessionJsonContext : JsonSerializerContext { }