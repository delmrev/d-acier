using System.Net;
using System.Net.Sockets;
using NLog;

public class ProcessIncoming
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public async Task<byte[]?> Process(UdpReceiveResult request, IPEndPoint serverAddress)
    {
        StunPacket packet = new(request.Buffer);
        switch (packet.Type)
        {
            case MessageType.BindingRequest:
                var response = await BindingRequest(request,packet,serverAddress, request.RemoteEndPoint);
                if(response == null)
                {
                    return null;
                } else
                {
                    return response.ToBytes();
                }
            default:
                Log.Warn($"Unknown MessageType: {packet.Type}");
            break;

        }
        return null;
    }
    private async Task<StunPacket?> BindingRequest(UdpReceiveResult request, StunPacket requestpacket, IPEndPoint serverAddress, IPEndPoint clientAddress)
    {
        try
        {
            StunPacket response = new(MessageType.BindingSuccessResponse,requestpacket.TransactionID);
            response.Attributes.Add(await AddressAttribute.GetAddress(request.RemoteEndPoint,Attribute.MappedAddress));
            response.Attributes.Add(await AddressAttribute.GetXORMappedAddress(request));
            response.Attributes.Add(await AddressAttribute.GetAddress(serverAddress.Address.Equals(StunServerManager.Instance.Default) ? new IPEndPoint(IPAddress.Parse("178.32.126.73"),serverAddress.Port) : serverAddress,Attribute.SourceAddress));
            response.Attributes.Add(await AddressAttribute.GetAddress(await StunServerManager.Instance.GetChangedAddress(serverAddress),Attribute.ChangedAddress));
            byte flag = requestpacket.Attributes[0].Value[3];
            if(flag == 0x0)
            {
                return response;
            } else
            {
                await StunServerManager.Instance.SendPacketByOptions(serverAddress,(flag & 0x04) != 0,(flag & 0x02) != 0,clientAddress,response.ToBytes());
                return null;
            }
        }
        catch(Exception ex)
        {
            Log.Error(ex);
            return null;
        }
    }
}