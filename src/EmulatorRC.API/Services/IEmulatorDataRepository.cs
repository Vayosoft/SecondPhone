using System;
namespace MySignalRTest.Services
{
    public interface IEmulatorDataRepository
    {
        
        string? GetLastScreenId(string deviceId);
        byte[]? GetLastScreen(string deviceId);
        byte[]? GetScreen(string deviceId, string id);
        void SetLastScreenId(string deviceId, string id);
        void SetLastScreen(string deviceId, byte[] screen);
        void SetScreen(string deviceId, string id, byte[] screen);
        
        

    }
}

