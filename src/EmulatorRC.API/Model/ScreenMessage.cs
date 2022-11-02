namespace EmulatorRC.API.Model
{
    public class ScreenMessage
    {
        public string Id { get; set; } = null!;
        public byte[] Image { get; init; } = null!;
    }
}
