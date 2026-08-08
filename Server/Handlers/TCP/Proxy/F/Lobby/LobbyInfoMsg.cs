using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class LobbyInfoMsg : IFPacketHandler
    {
        private readonly LeaveLobby _leaveLobby = new();
        public async Task Process(FPacket fPacket, Session session)
        {
            if(session.currentRoom is null)
            {
                return;
            }
            var data = Reader.ReadBytes(fPacket.payload, "BBHLLQ");
            switch ((byte)data[0]){
                case 0x46: // F disconnect
                    await _leaveLobby.Process(fPacket,session);
                break;
                case 0x49: // kick
                    int userId = (int)data[3];
                    session.currentRoom.Users[userId].currentRoom = null;
                    session.currentRoom.Users.Remove(userId);
                    var buffer = Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.Kick_2, StatusCode.Success, 14754, userId, 46117438, (long)data[5]);
                    FPacket response = new(fPacket.channel,(byte)FClientOpcode.LobbyMessage, buffer);
                    foreach(var user in session.currentRoom.Users)
                    {
                        await user.Value.Send(response.ToBytes());
                    }   
                    if(session.currentRoom.Users[userId].channels.TryGetValue("Relay.1", out int value))
                    {
                        GPacket gPacket = new(value);
                        await session.currentRoom.Users[userId].Send(gPacket.ToBytes());
                    }
                    if(session.currentRoom.Users[userId].channels.TryGetValue("ath", out int value1))
                    {
                        GPacket gPacket = new(value1);
                        await session.currentRoom.Users[userId].Send(gPacket.ToBytes());
                    }
                break;
            }
        }
    }
}