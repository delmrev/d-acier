using System.Buffers.Binary;
using NLog;

public static class UserData
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public async static Task Process(FPacket fPacket, Session session)
    {
        var data = await Reader.ReadBytes(fPacket.payload,"Q");
        if(session.currentRoom != null)
        {
            var user = session.currentRoom.Users.FirstOrDefault(r => (long)data[0] == r.EugenID);
            if(user != null)
            {
                Log.Info($"Data user {session.EugenID} -> {user.EugenID}");
                BinaryPrimitives.WriteInt64BigEndian(fPacket.payload.AsSpan(0, 8), user.EugenID);
                await ProxyReader.FinalizePacket(await fPacket.ToSend(),user);
            } else
            {
                Log.Error($"User dont found: {data[0]}");
            }
        } else
        {
            Log.Error("Try to send message to user but lobby is null");
        }
    }
}