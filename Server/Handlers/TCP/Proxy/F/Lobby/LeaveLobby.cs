using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class LeaveLobby : IFPacketHandler
    {
        public async Task Process(FPacket packet, Session session)
        {
            if(session.currentRoom == null )
            {
                return;
            }
            var read = Reader.ReadBytes(packet.payload,"BBHLLQ");
            if((long)read[5] != session.currentRoom.ID)
            {
                var buf = Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.Disconnect, StatusCode.Success, 14754, session.roomKeyID, 46117438, (long)read[5]);
                FPacket response_2 = new(packet.channel,(byte)FClientOpcode.LobbyMessage, buf);
                await session.Send(response_2.ToBytes());
                return;
            }
            var buffer = Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.Disconnect, StatusCode.Success, 14754, session.roomKeyID, 46117438, session.currentRoom.ID);
            FPacket response = new(packet.channel,(byte)FClientOpcode.LobbyMessage, buffer);
            foreach(var user in session.currentRoom.Users)
            {
                await user.Value.Send(response.ToBytes());
            }
            session.currentRoom.Users.Remove(session.roomKeyID);
            if(session.currentRoom.Users.Count == 0)
            {
                await LobbyManager.Instance.RemoveRoom(session.currentRoom.ID, session.game_id);
                session.currentRoom.Dispose();
                session.currentRoom = null;
            } else if(session.currentRoom.Host == session && session.currentRoom.Users.Count > 0){
                int id = -1;
                foreach(var user in session.currentRoom.Users)
                {
                    id = user.Key;
                    session.currentRoom.Host = user.Value;
                    break;
                }
                buffer = Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.MessageHostChanged, StatusCode.Success, 14754, id, 46117438, session.currentRoom.ID);
                response = new(packet.channel,(byte)FClientOpcode.LobbyMessage, buffer);
                foreach(var user in session.currentRoom.Users)
                {
                    await user.Value.Send(response.ToBytes());
                }   
                session.currentRoom = null;
            } else
            {
                session.currentRoom = null;
            }
            if(session.channels.TryGetValue("Relay.1", out int value))
            {
                GPacket gPacket = new(value);
                await session.Send(gPacket.ToBytes());
                session.channels.Remove("Relay.1");
            }
            session.isConnectedToRelay = false;
        }
    }
}