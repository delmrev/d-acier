public static class LobbyInfoMsg
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        if(session.currentRoom is null)
        {
            return;
        }
        var data = await Reader.ReadBytes(fPacket.payload, "BBHLLQ");
        switch ((byte)data[0]){
            case 0x46: // F disconnect
                var buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.Disconnect, StatusCode.Success, 14754, 2, 46117438, (long)data[5]);
                FResponse response = new(fPacket.channel,FClientOpcode.LobbyMessage, buffer);
                for (int i = 0; i < session.currentRoom.Users.Count; i++)
                {
                    await ProxyReader.FinalizePacket(await response.ToSend(),session.currentRoom.Users[i]);
                }
                session.currentRoom.Users.Remove(session);
                if(session.currentRoom.Host == session && session.currentRoom.Users.Count > 1){
                    buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.MessageHostChanged, StatusCode.Success, 14754, 6, 46117438, (long)data[5]);
                    response = new(fPacket.channel,FClientOpcode.LobbyMessage, buffer);
                    session.currentRoom.Host = session.currentRoom.Users[0];
                    for (int i = 0; i < session.currentRoom.Users.Count; i++)
                    {
                        await ProxyReader.FinalizePacket(await response.ToSend(),session.currentRoom.Users[i]);
                    }   
                    session.currentRoom = null;
                } else
                {
                    await Global.RemoveRoom(session.currentRoom.ID, session.game_id);
                    session.currentRoom = null;
                }
            break;
        }
    }
}