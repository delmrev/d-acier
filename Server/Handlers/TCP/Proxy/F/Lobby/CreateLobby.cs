using NLog;

public static class CreateLobby
{
    private static long TotalRooms = 0;
    public static async Task Process(FPacket fPacket, Session session)
    {
        FResponse fResponse;
        List<byte> buffer;
        var values = await Reader.ReadBytes(fPacket.payload, "BBBBIIQ");
        byte header = Convert.ToByte(2); //values[4] - lobby size
        long newid = ((long)header << 56) | (++TotalRooms & 0x00FFFFFFFFFFFFFFL);
        session.currentRoom = new Lobby(session,newid);
        session.roomKeyID = 2;
        buffer = await Writer.WriteBytes("BBHLLQ", 0x64, StatusCode.Success, 14754, -1 , values[5], session.currentRoom.ID); // 0x64 - d - CreateLobby; 
        fResponse = new(fPacket.channel, FClientOpcode.SystemMessage, buffer);
        await ProxyReader.FinalizePacket(await fResponse.ToSend(), session);
        var conf = Global.GetConfigData();
        if(conf is null || conf?.Server.Address is null)
        {
            return;
        }
        var ipStr = conf.Server.Address.Split('.');
        byte[] byteip = [byte.Parse(ipStr[0]), byte.Parse(ipStr[1]),byte.Parse(ipStr[2]),byte.Parse(ipStr[3])];
        Array.Reverse(byteip);
        buffer = await Writer.WriteBytes("BBHLLQs", 0x66, 0x00, 0, BitConverter.ToInt32(byteip),-1905684631, session.currentRoom.ID, "Relay.1");
        fResponse = new(fPacket.channel, FClientOpcode.SystemMessage_2, buffer);
        await ProxyReader.FinalizePacket(await fResponse.ToSend(), session);
        buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.MessageHostChanged, StatusCode.Success, 14754, 2, -1, session.currentRoom.ID); // h - 0x68
        fResponse = new(fPacket.channel, FClientOpcode.LobbyMessage, buffer);
        await ProxyReader.FinalizePacket(await fResponse.ToSend(), session);
        buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.LobbyEnterFinished, StatusCode.Success, 14754, 2, -1, session.currentRoom.ID); // j - 0x6A
        fResponse = new(fPacket.channel, FClientOpcode.LobbyMessage, buffer);
        await ProxyReader.FinalizePacket(await fResponse.ToSend(), session);
        await Global.AddRoom(session.currentRoom, session.game_id);
        session.currentRoom.Users.Add(2,session);
    }
}