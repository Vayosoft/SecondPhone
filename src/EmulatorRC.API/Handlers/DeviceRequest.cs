namespace EmulatorRC.API.Handlers
{
    public record DeviceRequest(string DeviceId);
    public record SpeakerDeviceRequest(string DeviceId) : DeviceRequest(DeviceId);
    public record AudioDeviceRequest(string DeviceId) : DeviceRequest(DeviceId);
    public record VideoDeviceRequest(string DeviceId) : DeviceRequest(DeviceId)
    {
        public int Width { get; init; }
        public int Height { get; init; }
    }
}
