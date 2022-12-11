using Commons.Core.Application;
using Commons.Core.Application.CacheValue;
using EmulatorRC.Entities;
using EmulatorRC.Services;

namespace EmulatorRC.API.Model.Bridge.Helpers;


public static class CacheValueGetter
{
    public static Screen Screen(this ICacheValueGetter getter, string screenId)
    {
        return getter.Get<Screen>(getter.MemoryCache, screenId);
    }

    public static int ClientPort(this ICacheValueGetter getter, string deviceId, int bridgePort)
    {
        return getter.Get<int>(getter.MemoryCache, deviceId, bridgePort);
    }


    public static DeviceSession DeviceSession(this ICacheValueGetter getter, string bridgeSideName, string clientIp, int clientPort)
    {
        return getter.Get<DeviceSession>(getter.MemoryCache, bridgeSideName, clientIp, clientPort);
    }

}


public static class CacheValueSetter
{
    public static void Screen(this ICacheValueSetter setter, Screen screen)
    {
        setter.Set(setter.MemoryCache, screen, TimeSpans.TwoMinutes, screen.Id);
    }

    public static void ClientPort(this ICacheValueSetter setter, string deviceId, int bridgePort, int clientPort)
    {
        setter.SetWithSliding(setter.MemoryCache, clientPort, TimeSpans.TwoMinutes, deviceId, bridgePort);
    }

    /*public static void DeviceSession(this ICacheValueSetter setter, string bridgeSideName, string clientIp, int clientPort)
    {
        setter.SetWithSliding(setter.MemoryCache, new DeviceSession
        {
            BridgeSideName = bridgeSideName,
            ClientIp = clientIp,
            ClientPort = clientPort 
        }, TimeSpans.TwoMinutes, bridgeSideName, clientIp, clientPort);
    }*/


}



public static class CacheValueRemover
{
    public static void Screen(this ICacheValueRemover remover,  string screenId)
    {
        remover.Remove<Screen>(remover.MemoryCache, screenId);
    }
}