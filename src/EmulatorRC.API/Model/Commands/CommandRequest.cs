namespace EmulatorRC.API.Model.Commands
{
    public record CommandRequest(string DeviceId);
    public record AudioCommand(string DeviceId) : CommandRequest(DeviceId);
    public abstract record CameraCommand() : CommandRequest("")
    {
        public int Width { get; init; }
        public int Height { get; init; }
    }

    public record CameraFrontCommand : CameraCommand;

    public record CameraRearCommand : CameraCommand;


}
