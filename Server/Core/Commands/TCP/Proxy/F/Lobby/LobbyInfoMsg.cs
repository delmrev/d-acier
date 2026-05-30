public static class LobbyInfoMsg
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        if(session.currentRoom is null)
        {
            return;
        }
        var data = Reader.ReadBytes(fPacket.payload, "BBHLLQ");
        switch ((byte)data[0]){
            case 0x46: // F disconnect
                var buffer = Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.DISCONNECT, StatusCode.SUCCESS, 14754, 2, 46117438, (long)data[5]);
                FResponse response = new(fPacket.channel,FClientOpcode.LobbyMessage, buffer);
                for (int i = 0; i < session.currentRoom.Users.Count; i++)
                {
                    await ProxyReader.FinalizePacket(response.ToSend(),session.currentRoom.Users[i]);
                }
                session.currentRoom.Users.Remove(session);
                if(session.currentRoom.Host == session && session.currentRoom.Users.Count > 1){
                    buffer = Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.MESSAGE_HOST_CHANGED, StatusCode.SUCCESS, 14754, 6, 46117438, (long)data[5]);
                    response = new(fPacket.channel,FClientOpcode.LobbyMessage, buffer);
                    session.currentRoom.Host = session.currentRoom.Users[0];
                    for (int i = 0; i < session.currentRoom.Users.Count; i++)
                    {
                        await ProxyReader.FinalizePacket(response.ToSend(),session.currentRoom.Users[i]);
                    }   
                    session.currentRoom = null;
                } else
                {
                    Global.RemoveRoom(session.currentRoom.ID);
                    session.currentRoom = null;
                }
            break;
        }
    }
}