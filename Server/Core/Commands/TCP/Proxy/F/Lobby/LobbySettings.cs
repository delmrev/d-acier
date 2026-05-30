using NLog;

public static class LobbySettings
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static async Task Process(FPacket fPacket, Session session)
    {
        object[] values;
        if(session.currentRoom is null)
        {
            Log.Error("Try to get current room but dont have current room");
            return;
        }
        values = Reader.ReadBytes(fPacket.payload, "QIBc");
        if((byte)values[2] == 0x01) // if flag, dont save
        {
            Log.Debug($"Dont save; id: {values[1]}, value: {values[3]}");
            return;
        }
        int id = (int)values[1];
        string value = (string)values[3];
        if (!session.currentRoom.RoomSettings.TryAdd(id, value))
        {
            if(value == session.currentRoom.RoomSettings[id])
            {
                return;
            }
            session.currentRoom.RoomSettings[id] = value;
        }
        var buf = Writer.WriteBytes("QIBc",session.currentRoom.ID,id,(byte)values[2],value);
        FResponse response = new(fPacket.channel,FClientOpcode.LobbyInfo,buf);
        for (int i = 0; i < session.currentRoom.Users.Count; i++)
        {
            await ProxyReader.FinalizePacket(response.ToSend(),session.currentRoom.Users[i]);
        }
    }
}