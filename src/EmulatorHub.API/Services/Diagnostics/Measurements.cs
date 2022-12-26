using Vayosoft.Threading.Channels.Models;

namespace EmulatorHub.API.Services.Diagnostics
{
    public class Measurements
    {
        public SnapshotTime SnapshotTime { set; get; }
        public Dictionary<string, ChannelHandlerTelemetrySnapshot> Channels { set; get; }
        public double CpuUsage { set; get; }
    }
}
