namespace EmulatorRC.Services
{
    public interface IEmulatorDataRepository
    {
        
        string? GetLastScreenId(string deviceId);
        Screen? GetLastScreen(string deviceId);
        byte[]? GetScreen(string deviceId, string id);
        void SetLastScreenId(string deviceId, string id);
        void SetLastScreen(string deviceId, Screen screen);
        void SetScreen(string deviceId, string id, byte[] screen);
        
        

    }
}

