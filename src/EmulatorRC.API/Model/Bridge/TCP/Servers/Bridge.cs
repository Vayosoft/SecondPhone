using System.Net;
using Commons.Core.Cache;
using Commons.Core.Helpers;

namespace EmulatorRC.API.Model.Bridge.TCP.Servers;

public class Bridge
{
    public TcpBridgeServer Outer { get; set; }
    public TcpBridgeServer Inner { get; set; }


    public Bridge(string name,
        TcpBridgeServer outer,
        TcpBridgeServer inner,
        IPAddress address,
        int port,
        ILoggerFactory logger,
        IHostApplicationLifetime lifeTime,
        IPubSubCacheProvider redis,
        IMemoryCacheProvider mcache


    ) 
    {
        /*Name = Guard.NotEmpty(name, nameof(name));
        _logger = logger.CreateLogger(Name);
        _lifeTime = lifeTime;*/

    }



}


// public record TcpData(byte[] Buffer, long Offset, long Length);
