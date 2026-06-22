using EugnetProtocol.Common.Interfaces;
using EugnetProtocol.TCP.Proxy;
using NLog;

public class ProxyManager
{
    private readonly Dictionary<byte, IProxyHandler> _handlers = new();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public ProxyManager()
    {
        _handlers.Add((byte)PacketType.OLDCONNECT,new OldConnect());
        _handlers.Add((byte)PacketType.CONNECT,new ConnectMessage());
        _handlers.Add((byte)PacketType.DATA,new FPacketManager());
        _handlers.Add((byte)PacketType.CONFIRM,new DPacketManager());
        _handlers.Add((byte)PacketType.CLOSE_CHANNEL, new CloseChannel());
    }
    public async Task Handle(byte[] data, Session session)
    {
        if(_handlers.TryGetValue(data[0], out var handler))
        {
            await handler.Process(data,session);
        } else
        {
            Log.Warn($"Unknown opcode: {data[0]}");
        }
    }
}