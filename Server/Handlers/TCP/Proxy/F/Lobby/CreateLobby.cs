using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class CreateLobby : IFPacketHandler
    {
        private static long TotalRooms = 0;
        public async Task Process(FPacket fPacket, Session session)
        {
            FPacket fResponse;
            List<byte> buffer;
            var values = await Reader.ReadBytes(fPacket.payload, "BBBBIIQ");
            byte header = Convert.ToByte(2); //values[4] - lobby size
            long newid = ((long)header << 56) | (++TotalRooms & 0x00FFFFFFFFFFFFFFL);
            session.currentRoom = new Lobby(session,newid);
            session.roomKeyID = 2;
            buffer = await Writer.WriteBytes("BBHLLQ", 0x64, StatusCode.Success, 14754, -1 , values[5], session.currentRoom.ID); // 0x64 - d - CreateLobby; 
            fResponse = new(fPacket.channel, (byte)FClientOpcode.SystemMessage, buffer);
            await session.Send(await fResponse.ToSend());
            var conf = GlobalManager.GetConfigData();
            if(conf is null || conf?.Server.Address is null)
            {
                return;
            }
            var ipStr = conf.Server.Address.Split('.');
            byte[] byteip = [byte.Parse(ipStr[0]), byte.Parse(ipStr[1]),byte.Parse(ipStr[2]),byte.Parse(ipStr[3])];
            Array.Reverse(byteip);
            buffer = await Writer.WriteBytes("BBHLLQs", 0x66, 0x00, 0, BitConverter.ToInt32(byteip),-1905684631, session.currentRoom.ID, "Relay.1");
            fResponse = new(fPacket.channel, (byte)FClientOpcode.SystemMessage_2, buffer);
            await session.Send(await fResponse.ToSend());
            buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.MessageHostChanged, StatusCode.Success, 14754, 2, -1, session.currentRoom.ID); // h - 0x68
            fResponse = new(fPacket.channel, (byte)FClientOpcode.LobbyMessage, buffer);
            await session.Send(await fResponse.ToSend());
            buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.LobbyEnterFinished, StatusCode.Success, 14754, 2, -1, session.currentRoom.ID); // j - 0x6A
            fResponse = new(fPacket.channel, (byte)FClientOpcode.LobbyMessage, buffer);
            await session.Send(await fResponse.ToSend());
            await GlobalManager.AddRoom(session.currentRoom, session.game_id);
            session.currentRoom.Users.Add(2,session);
        }
    }
}