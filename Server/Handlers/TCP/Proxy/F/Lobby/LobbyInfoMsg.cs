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
                await LeaveLobby.Process(session);
            break;
            case 0x49: // kick
                int userId = (int)data[3];
                session.currentRoom.Users[userId].currentRoom = null;
                session.currentRoom.Users.Remove(userId);
                var buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.Kick_2, StatusCode.Success, 14754, userId, 46117438, (long)data[5]);
                FResponse response = new(fPacket.channel,FClientOpcode.LobbyMessage, buffer);
                foreach(var user in session.currentRoom.Users)
                {
                    await ProxyReader.FinalizePacket(await response.ToSend(),user.Value);
                }   
            break;
        }
    }
}