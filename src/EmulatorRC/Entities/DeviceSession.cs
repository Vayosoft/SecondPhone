using System.Text.Json.Serialization;

namespace EmulatorRC.Entities;

public class DeviceSession
{
    public string DeviceId { get; set; } = null!;

    public string? AccessToken { get; set; }

    public string? StreamType { get; set; }
}

[JsonSerializable(typeof(DeviceSession))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
public partial class DeviceSessionJsonContext : JsonSerializerContext { }