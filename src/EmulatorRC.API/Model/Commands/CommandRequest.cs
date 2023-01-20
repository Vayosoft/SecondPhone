namespace EmulatorRC.API.Model.Commands
{
    public record CommandRequest(string DeviceId);
    public record SpeakerCommand(string DeviceId) : CommandRequest(DeviceId);
    public record AudioCommand(string DeviceId) : CommandRequest(DeviceId);
    public record VideoCommand(string DeviceId) : CommandRequest(DeviceId)
    {
        public int Width { get; init; }
        public int Height { get; init; }
    }
}
