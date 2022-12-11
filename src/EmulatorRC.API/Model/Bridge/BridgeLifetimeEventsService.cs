using System.Net;
using Commons.Core.Application;
using EmulatorRC.API.Channels;
using EmulatorRC.API.Model.Bridge.Config;
using EmulatorRC.API.Model.Bridge.TCP.Servers;
using EmulatorRC.API.Model.Bridge.TCP.Interfaces;

namespace EmulatorRC.API.Model.Bridge;

public class BridgeLifetimeEventsService : IHostedService
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly TcpStreamChannel _streamChannel;
    private readonly IHostApplicationLifetime _lifeTime;
    private readonly ApplicationCache _appCache;

    private readonly List<IBridgeServer> _bridgeServers;

    public BridgeLifetimeEventsService(
        IHostApplicationLifetime hostApplicationLifetime,
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        TcpStreamChannel streamChannel,
        ApplicationCache appCache
        )
    {
        _lifeTime = hostApplicationLifetime;
        _loggerFactory = loggerFactory;
        _configuration = configuration;
        _streamChannel = streamChannel;
        _appCache = appCache;
        _bridgeServers = new List<IBridgeServer>();
    }


    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifeTime.ApplicationStarted.Register(OnStarted);
        _lifeTime.ApplicationStopping.Register(OnStopping);
        _lifeTime.ApplicationStopped.Register(OnStopped);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void OnStarted()
    {
        var bo = new BridgeOptions();
        _configuration.GetSection(BridgeOptions.Bridge).Bind(bo);

        var outerSide = $"{bo.Name}.outer.{bo.Outer.TcpPort}";
        var innerSide = $"{bo.Name}.inner.{bo.Inner.TcpPort}";
        var bufferSize = 128 * 1024 * 1024;

        _bridgeServers.Add(new TcpBridgeServer(
            BridgeRole.Outer,
            outerSide,
            innerSide,
            IPAddress.Any,
            bo.Outer.TcpPort,
            bufferSize,
            _loggerFactory,
            _streamChannel,
            _lifeTime,
            _appCache)
        {
            OptionNoDelay = true,
            OptionKeepAlive = true
        });
        
        _bridgeServers.Add(new TcpBridgeServer(
            BridgeRole.Inner,
            innerSide,
            outerSide,
            IPAddress.Any,
            bo.Inner.TcpPort,
            bufferSize,
            _loggerFactory,
            _streamChannel,
            _lifeTime,
            _appCache)
        {
            OptionNoDelay = true,
            OptionKeepAlive = true
        });

        _bridgeServers.ForEach(s =>
        {
            s.Start();
            while (!s.IsStarted)
                Thread.Yield();
        });
    }

    private void OnStopping()
    {
        _bridgeServers.ForEach(s =>
        {
            try
            {
                s.Stop();
            }
            catch (Exception) { /* ignored */ }
        });
    }

    private void OnStopped()
    {
        // ...
    }
}

public enum BridgeRole
{
    Outer,
    Inner
}