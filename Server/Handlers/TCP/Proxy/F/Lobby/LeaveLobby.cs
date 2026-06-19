public class LeaveLobby()
{
    public static async Task Process(Session session)
    {
        var buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.Disconnect, StatusCode.Success, 14754, session.roomKeyID, 46117438, session.currentRoom);
        FResponse response = new(1,FClientOpcode.LobbyMessage, buffer);
        foreach(var user in session.currentRoom.Users)
        {
            await ProxyReader.FinalizePacket(await response.ToSend(),user.Value);
        }
        session.currentRoom.Users.Remove(session.roomKeyID);
        if(session.currentRoom.Host == session && session.currentRoom.Users.Count > 0){
            int id = -1;
            foreach(var user in session.currentRoom.Users)
            {
                id = user.Key;
                session.currentRoom.Host = user.Value;
                break;
            }
            buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.MessageHostChanged, StatusCode.Success, 14754, id, 46117438, session.currentRoom);
            response = new(1,FClientOpcode.LobbyMessage, buffer);
            foreach(var user in session.currentRoom.Users)
            {
                await ProxyReader.FinalizePacket(await response.ToSend(),user.Value);
            }   
            session.currentRoom = null;
        } else
        {
            await Global.RemoveRoom(session.currentRoom.ID, session.game_id);
            session.currentRoom = null;
        }
    }
}