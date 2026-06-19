using NLog;

public static class CurrentState
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public async static Task Process(FPacket fPacket, Session session)
    {  
        var data = await Reader.TryReadBytes(fPacket.payload, "IIBBHIII");
        session.unk_1 = (int)data.output[0];
        session.unk_2 = (int)data.output[1];
        if(session.currentRoom is not null)
        {
            byte[] result = fPacket.payload
                        .Concat(BitConverter.GetBytes(session.EugenID))
                        .Concat(fPacket.payload.Skip(1))
                        .ToArray();
            FResponse response = new(fPacket.channel, FClientOpcode.BM_FRIEND_PRESENCE, [..result]);
            foreach(var user in session.currentRoom.Users)
            {
                await ProxyReader.FinalizePacket(await response.ToSend(),user.Value);
            }
        }
    }
}