using System.Net;
using System.Net.Sockets;
using NLog;

public class UdpServer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();      
    private UdpClient server;
    private CancellationTokenSource _cts = new();
    private ProcessIncoming parser = new();
    public IPEndPoint EndPoint {get; private set;}
    public UdpServer(string ip, ushort port)
    {
        IPEndPoint endPoint = new(IPAddress.Parse(ip),port);
        EndPoint = endPoint;
        server = new(endPoint);
    }
    public UdpServer(IPEndPoint endPoint)
    {
        EndPoint = endPoint;
        server = new(endPoint);
    }
    public async Task Start()
    {
        while (!_cts.IsCancellationRequested)
        {
            var result = await server.ReceiveAsync();

            _ = ProcessAndSendAsync(result);
        }
    }
    private async Task ProcessAndSendAsync(UdpReceiveResult result)
    {
        try
        {
            var response = await parser.Process(result, EndPoint);
            if (response != null)
            {
                await server.SendAsync(response, result.RemoteEndPoint,_cts.Token);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
    }
    public async Task SendPacket(byte[] bytes, IPEndPoint client)
    {
        await server.SendAsync(bytes,client);
    }
}