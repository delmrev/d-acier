// Dont work because want steam friends
public class GetFriends
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        using MemoryStream stream = new(fPacket.payload);
        using BinaryReader reader = new(stream);
        var settings = Reader.ReadBytes(reader,"H");
        int count = 0;
        List<byte> buffer = new();
        for (int i = 0; i < (short)settings[0]; i++)
        {
            var buf = Reader.ReadBytes(reader, "BS");
            var user = await DatabaseManager.GetU0BySteamID(long.Parse((string)buf[1]));
            if(user == null)
            {
                continue;
            } else
            {
                count++;
                buffer.AddRange(Writer.WriteBytes("BQBS",0x1,user.EugenID,0x0,(string)buf[1]));
            }
        }
        buffer.InsertRange(0,Writer.WriteBytes("H",count));
        FResponse response = new(fPacket.channel, FClientOpcode.NETWORK_CHANNEL_FRIEND_ADD_EXTERNAL_API_FRIEND,buffer);
        await ProxyReader.FinalizePacket(response.ToSend(),session);
    }
}