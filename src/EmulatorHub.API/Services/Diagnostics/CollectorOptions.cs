namespace EmulatorHub.API.Services.Diagnostics
{
    public class CollectorOptions
    {
        public long CollectIntervalMilliseconds { set; get; } = 60 * 1000;
    }
}
