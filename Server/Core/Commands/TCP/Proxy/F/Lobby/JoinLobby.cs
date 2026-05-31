public class JoinLobby()
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        var data = await Reader.ReadBytes(fPacket.payload,"BBHLLQ");
        FResponse response;
        List<byte> buffer;
        long roomID = (long)data[5];
        Room room = Global.GetRoom(roomID);
        room.Users.Add(session);
        session.currentRoom = room;
        foreach (var option in room.RoomSettings)
        {
            var buf = await Writer.WriteBytes("QIBc",roomID,option.Key,0x00,option.Value);
            response = new(fPacket.channel,FClientOpcode.LobbyInfo,buf);
            await ProxyReader.FinalizePacket(await response.ToSend(),session);
        }
        buffer = await Writer.WriteBytes("BBHQLQ", LobbyCommandsClient.CONNECT, 0x00, 1, room.ID, 2, room.Host.EugenID);
        response = new(fPacket.channel, FClientOpcode.LobbyMessage, buffer);
        await ProxyReader.FinalizePacket(await response.ToSend(),session);
        buffer = await Writer.WriteBytes("BBHQLQ", LobbyCommandsClient.CONNECT, 0x00, 0, room.ID, 6, session.EugenID);
        response = new(fPacket.channel, FClientOpcode.LobbyMessage, buffer);
        await ProxyReader.FinalizePacket(await response.ToSend(),room.Host);
        buffer = await Writer.WriteBytes("BBHLLQ", SystemMessageType.ON_LOBBY_ENTERED, 0x00, 0, -1, 0, room.ID);
        response = new(fPacket.channel, FClientOpcode.SystemMessage, buffer);
        await ProxyReader.FinalizePacket(await response.ToSend(),session);
        var conf = Global.GetConfigData();
        if(conf is null || conf?.ip is null)
        {
            return;
        }
        var ipStr = conf?.ip.Split('.');
        byte[] byteip = [byte.Parse(ipStr[0]), byte.Parse(ipStr[1]),byte.Parse(ipStr[2]),byte.Parse(ipStr[3])];
        Array.Reverse(byteip);
        buffer = await Writer.WriteBytes("BBHLLQs", 0x66, 0x00, 0, BitConverter.ToInt32(byteip),-1905684631, room.ID, "Relay.1");
        response = new(fPacket.channel, FClientOpcode.SystemMessage_2, buffer);
        await ProxyReader.FinalizePacket(await response.ToSend(), session);
        buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.MESSAGE_HOST_CHANGED, 0x00, 14754, 2, -1, room.ID); // h - 0x68
        response = new(fPacket.channel, FClientOpcode.LobbyMessage, buffer);
        await ProxyReader.FinalizePacket(await response.ToSend(), session);
        buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.LOBBY_ENTER_FINISHED,  0x00, 14754, 6, -1, room.ID); // j - 0x6A
        response = new(fPacket.channel, FClientOpcode.LobbyMessage, buffer);
        await ProxyReader.FinalizePacket(await response.ToSend(), session);
        buffer = await Writer.WriteBytes("BBHLLQ", SystemMessageType.JOIN_LOBBY_FINISHED, 0x00, 0, 6, 1, room.ID); 
        response = new(fPacket.channel, FClientOpcode.SystemMessage, buffer);
        await ProxyReader.FinalizePacket(await response.ToSend(),room.Host);
    }
}