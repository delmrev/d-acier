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
            var read = await Reader.ReadBytes(packet.payload,"BBHLLQ");
            if((long)read[5] != session.currentRoom.ID)
            {
                var buf = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.Disconnect, StatusCode.Success, 14754, session.roomKeyID, 46117438, (long)read[5]);
                FPacket response_2 = new(packet.channel,(byte)FClientOpcode.LobbyMessage, buf);
                await session.Send(await response_2.ToSend());
                return;
            }
            var buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.Disconnect, StatusCode.Success, 14754, session.roomKeyID, 46117438, session.currentRoom.ID);
            FPacket response = new(packet.channel,(byte)FClientOpcode.LobbyMessage, buffer);
            foreach(var user in session.currentRoom.Users)
            {
                await user.Value.Send(await response.ToSend());
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
                buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.MessageHostChanged, StatusCode.Success, 14754, id, 46117438, session.currentRoom.ID);
                response = new(packet.channel,(byte)FClientOpcode.LobbyMessage, buffer);
                foreach(var user in session.currentRoom.Users)
                {
                    await user.Value.Send(await response.ToSend());
                }   
                session.currentRoom = null;
            } else
            {
                session.currentRoom = null;
            }
            if(session.channels.TryGetValue("Relay.1", out int value))
            {
                GPacket gPacket = new(value);
                await session.Send(await gPacket.ToSend());
                session.channels.Remove("Relay.1");
            }
            session.isConnectedToRelay = false;
        }
    }
}