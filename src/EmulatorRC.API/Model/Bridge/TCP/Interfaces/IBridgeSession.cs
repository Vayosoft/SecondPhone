namespace EmulatorRC.API.Model.Bridge.TCP.Interfaces
{
    public interface IBridgeSession : IDisposable
    {
        string ClientIp { get; }
        int ClientPort { get; }
        string ClientId { get; }
        
        bool SendAsync(byte[] buffer, long offset, long size);
        
        bool SendAsync(string text);

        bool Disconnect();
        
        bool IsConnected { get; }

        // void OnConnected();

    }
    
}