namespace EmulatorRC.API.Model.Bridge.TCP.Interfaces
{
    public interface IBridgeServer
    {
        bool Start();
        bool Stop();
        bool IsStarted { get; }
    }

}