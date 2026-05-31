using NLog;
public class ProxyReader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    public static async Task ProcessPacket(byte[] packet, Session session)
    {
        using var memoryStream = new MemoryStream(packet);
        using var reader = new BinaryReader(memoryStream);
        switch (packet[0])
        {
            case (byte)PacketType.CONNECT: // c - Connect: Income: 0х42. Server responce: size, opcode (с), EugenID, unknown, unknown, unknown, unknown
                await ConnectMessage.Process(packet, session);
                byte opcode = packet[0];
                byte[] channel = packet[1..5];
                byte[] payloadLengthBytes = packet[5..7];
                byte method = packet[7];

                Log.Debug("RX Opcode {0} channel {1} payload length {2} method {3}",
                    opcode.ToString("X2"),
                    BitConverter.ToString(channel),
                    BitConverter.ToString(payloadLengthBytes),
                    method.ToString("X2")
                );
                break;
            case (byte)PacketType.CONFIRM: //d (0x64) - connection handler: Length, opcode (d), channel, s (length, command). Server response: length, channel, I command (affects response content)
                await DReader.ProcessPacket(packet, session);
                break;
            case (byte)PacketType.DATA: //f - Data packet, FPacket.cs
                var Fpacket = new FPacket(packet);
                await FReader.ProcessFPacket(Fpacket, session);
                opcode = packet[0];
                channel = packet[1..5];
                payloadLengthBytes = packet[5..7];
                method = packet[7];

                    Log.Debug("RX Opcode {0} channel {1} payload length {2} method {3}",
                        opcode.ToString("X2"),
                        BitConverter.ToString(channel),
                        BitConverter.ToString(payloadLengthBytes),
                        method.ToString("X2")
                    );
                break;
            case (byte)PacketType.CLOSE_CHANNEL: // g - Close connectiom, size, opcode, channel // I
                var data = await Reader.ReadBytes(packet,"BI");
                try
                {
                    session.channels.Remove((int)data[1]);
                }
                catch
                {
                    Log.Error("Try to remove non existing channel!");
                } finally
                {
                    if(session.channels.Count == 0)
                    {
                        session.Dispose();
                    }
                }
                break;
            case (byte)PacketType.EMERGENCY_DISCONNECT: //z - timeout: size, opcode
                session.Dispose();
                break;
        }
    }

    public static async Task FinalizePacket(List<byte> buffer, Session session)
    {
        var length = await Writer.WriteBytes("H", buffer.Count);
        buffer.InsertRange(0, length.ToArray());
        await session.Server.SendPacket(session.Ssl, buffer.ToArray());
    }
}