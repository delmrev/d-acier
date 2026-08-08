using System.Collections.Concurrent;
using System.Net;
using NLog;

public class StunServerManager
{
    private static readonly StunServerManager _instance = new();
    public static StunServerManager Instance => _instance;
    public IPAddress Default;
    private ConcurrentDictionary<IPEndPoint, UdpServer> servers = new();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public async Task Init(StunConfig config)
    {
        Log.Info($"Default: {config.Default}");
        Default = IPAddress.Parse(config.Default);
        if(config.Address.Length <= 1)
        {
            Log.Fatal("Config have 1 address");
            return;
        }
        foreach(var ip in config.Address)
        {
            var splitOptions = ip.Split(":", StringSplitOptions.RemoveEmptyEntries);
            if(splitOptions[0] == "0.0.0.0")
            {
                Log.Fatal("0.0.0.0 not allowed");
                return;
            }
            IPEndPoint endPoint = new(IPAddress.Parse(splitOptions[0]),ushort.Parse(splitOptions[1]));
            UdpServer server = new(endPoint);
            servers.TryAdd(endPoint,server);
            _ = Task.Run(() => server.Start());
            Log.Info($"STUN server on {endPoint.Address}:{endPoint.Port} started");
        }
    }
    public async Task<IPEndPoint> GetChangedAddress(IPEndPoint currentEndpoint)
    {
        foreach (var endPoint in servers)
        {
            if(endPoint.Key.Port != currentEndpoint.Port && !endPoint.Key.Address.Equals(currentEndpoint.Address))
            {
                return endPoint.Key;
            }
        }
        return currentEndpoint;
    }
    public async Task SendPacketByOptions(IPEndPoint currentEndpoint,
    bool changeIP, bool changePort,
     IPEndPoint clientAddress, byte[] packet)
    {
        foreach (var endPoint in servers)
        {
            if(changeIP && endPoint.Key.Address.Equals(currentEndpoint.Address))
            {
                continue;
            }
            if(changePort && endPoint.Key.Port == currentEndpoint.Port)
            {
                continue;
            }
            await endPoint.Value.SendPacket(packet,clientAddress);
            return;
        }
        Log.Warn("STUN server dont send packet");
    }
}