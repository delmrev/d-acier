using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

public class AddressAttribute
{
    private static readonly byte[] MagicCookie = [0x21, 0x12, 0xA4, 0x42];
    public async static Task<PacketAttribute> GetAddress(IPEndPoint endPoint, Attribute type)
    {
        byte IPType = 0x00;
        IPType = endPoint.AddressFamily switch 
        {
            AddressFamily.InterNetwork => 0x01,
            AddressFamily.InterNetworkV6 => 0x02,
            _ => 0x00
        };
        byte[] addressBytes = endPoint.Address.GetAddressBytes();
        int size = 4+addressBytes.Length;
        Span<byte> writingSpan = stackalloc byte[size];
        writingSpan.Clear();
        writingSpan[1] = IPType;
        BinaryPrimitives.WriteUInt16BigEndian(writingSpan.Slice(2,2),(ushort)endPoint.Port);
        addressBytes.CopyTo(writingSpan[4..]);
        PacketAttribute attribute = new()
        {
            Type = type,
            Value = writingSpan.ToArray()
        };
        return attribute;
    }
    public async static Task<PacketAttribute> GetXORMappedAddress(UdpReceiveResult request)
    {
        byte IPType = 0x00;
        byte[] addressBytes = request.RemoteEndPoint.Address.GetAddressBytes();
        switch (request.RemoteEndPoint.AddressFamily)
        {
            case AddressFamily.InterNetwork:
            IPType = 0x01;
            for (int i = 0; i < 4; i++)
            {
                addressBytes[i] = (byte)(addressBytes[i] ^ MagicCookie[i]);
            }
            break;
            case AddressFamily.InterNetworkV6:
            IPType = 0x02;
            for (int i = 0; i < 16; i++)
            {
                addressBytes[i] = (byte)(addressBytes[i] ^ request.Buffer[4 + i]);
            }
            break;
        }
        int size = 4+addressBytes.Length;
        Span<byte> writingSpan = stackalloc byte[size];
        writingSpan.Clear();
        writingSpan[1] = IPType;
        ushort port = (ushort)(((ushort)request.RemoteEndPoint.Port) ^ 0x2112);
        BinaryPrimitives.WriteUInt16BigEndian(writingSpan.Slice(2,2),port);
        addressBytes.CopyTo(writingSpan[4..]);
        PacketAttribute attribute = new()
        {
            Type = Attribute.XorMappedAddress,
            Value = writingSpan.ToArray()
        };
        return attribute;
    }
}