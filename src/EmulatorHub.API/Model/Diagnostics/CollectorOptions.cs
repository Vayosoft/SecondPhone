namespace EmulatorHub.API.Model.Diagnostics
{
    public class CollectorOptions
    {
        public long CollectIntervalMilliseconds { set; get; } = 60 * 1000;
    }
}
