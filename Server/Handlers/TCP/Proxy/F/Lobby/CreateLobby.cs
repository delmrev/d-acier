using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class CreateLobby : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            FPacket fResponse;
            byte[] buffer;
            var values =Reader.ReadBytes(fPacket.payload, "BBBBIIQ"); //values[4] - lobby size
            session.currentRoom = new Lobby(session,await LobbyManager.Instance.GetRoomID());
            session.roomKeyID = 2;
            buffer = Writer.WriteBytes("BBHLLQ", 0x64, StatusCode.Success, 14754, -1 , values[5], session.currentRoom.ID); // 0x64 - d - CreateLobby; 
            fResponse = new(fPacket.channel, (byte)FClientOpcode.SystemMessage, buffer);
            await session.Send(fResponse.ToBytes());
            buffer = Writer.WriteBytes("BBHLLQs", 0x66, 0x00, 0, -1306493367,-1905684631, session.currentRoom.ID, "Relay.1");
            fResponse = new(fPacket.channel, (byte)FClientOpcode.SystemMessage_2, buffer);
            await session.Send(fResponse.ToBytes());
            buffer = Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.MessageHostChanged, StatusCode.Success, 14754, 2, -1, session.currentRoom.ID);
            fResponse = new(fPacket.channel, (byte)FClientOpcode.LobbyMessage, buffer);
            await session.Send(fResponse.ToBytes());
            buffer = Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.LobbyEnterFinished, StatusCode.Success, 14754, 2, -1, session.currentRoom.ID);
            fResponse = new(fPacket.channel, (byte)FClientOpcode.LobbyMessage, buffer);
            await session.Send(fResponse.ToBytes());
            await LobbyManager.Instance.AddRoom(session.currentRoom, session.game_id);
            session.currentRoom.Users.Add(2,session);
        }
    }
}