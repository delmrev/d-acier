public class FriendCommand
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        List<byte> buffer = new();
        byte[] packet = [ //empty?
            0x09,0x00, // unknown
                    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        ];
        buffer.AddRange(packet);
        buffer.AddRange(await Writer.WriteBytes("Q", session.EugenID));
        packet = [0xFF, 0xFF, 0xFF, 0xFF];
        buffer.AddRange(packet);
        FResponse response = new(fPacket.channel, FClientOpcode.BM_TEAM_COMMAND, buffer);
        await ProxyReader.FinalizePacket(await response.ToSend(), session);
    }
}