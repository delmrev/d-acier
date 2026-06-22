using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class JoinLobby() : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            var data = await Reader.ReadBytes(fPacket.payload,"BBHLLQ");
            FPacket response;
            List<byte> buffer;
            long roomID = (long)data[5];
            Lobby room = await GlobalManager.GetRoom(roomID,session.game_id);
            int place = -1;
            for (int i = 2; i <= int.Parse(room.RoomSettings[2])*2; i+=2)
            {
                if (!room.Users.ContainsKey(i))
                {
                    place = i;
                    break;
                }
            }
            if(place == -1)
            {
                buffer = await Writer.WriteBytes("BBHLLQ", SystemMessageType.DisconnectFromMms, StatusCode.PendingClientListFull, 0, -1, 0, room.ID);
                response = new(fPacket.channel, (byte)FClientOpcode.SystemMessage, buffer);
                await session.Send(await response.ToSend());
                return;
            }
            session.roomKeyID = place;
            room.Users.Add(place,session);
            session.currentRoom = room;
            foreach (var option in room.RoomSettings)
            {
                var buf = await Writer.WriteBytes("QIBc",roomID,option.Key,0x00,option.Value);
                response = new(fPacket.channel,(byte)FClientOpcode.LobbyInfo,buf);
                await session.Send(await response.ToSend());
            }
            buffer = await Writer.WriteBytes("BBHQLQ", LobbyCommandsClient.Connect, StatusCode.Success, 1, room.ID, room.Host.roomKeyID, room.Host.EugenID);
            response = new(fPacket.channel, (byte)FClientOpcode.LobbyMessage, buffer);
            await session.Send(await response.ToSend());
            buffer = await Writer.WriteBytes("BBHQLQ", LobbyCommandsClient.Connect, StatusCode.Success, 0, room.ID, session.roomKeyID, session.EugenID);
            response = new(fPacket.channel, (byte)FClientOpcode.LobbyMessage, buffer);
            await room.Host.Send(await response.ToSend());
            buffer = await Writer.WriteBytes("BBHLLQ", SystemMessageType.OnLobbyEntered, StatusCode.Success, 0, -1, 0, room.ID);
            response = new(fPacket.channel, (byte)FClientOpcode.SystemMessage, buffer);
            await session.Send(await response.ToSend());
            var conf = GlobalManager.GetConfigData();
            if(conf is null || conf?.Server.Address is null)
            {
                return;
            }
            var ipStr = conf.Server.Address.Split('.');
            byte[] byteip = [byte.Parse(ipStr[0]), byte.Parse(ipStr[1]),byte.Parse(ipStr[2]),byte.Parse(ipStr[3])];
            Array.Reverse(byteip);
            buffer = await Writer.WriteBytes("BBHLLQs", 0x66, StatusCode.Success, 0, BitConverter.ToInt32(byteip),-1905684631, room.ID, "Relay.1");
            response = new(fPacket.channel, (byte)FClientOpcode.SystemMessage_2, buffer);
            await session.Send(await response.ToSend());
            buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.MessageHostChanged, StatusCode.Success, 14754, room.Host.roomKeyID, -1, room.ID); // h - 0x68
            response = new(fPacket.channel, (byte)FClientOpcode.LobbyMessage, buffer);
            await session.Send(await response.ToSend());
            buffer = await Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.LobbyEnterFinished,  StatusCode.Success, 14754, session.roomKeyID, -1, room.ID); // j - 0x6A
            response = new(fPacket.channel, (byte)FClientOpcode.LobbyMessage, buffer);
            await session.Send(await response.ToSend());
            buffer = await Writer.WriteBytes("BBHLLQ", SystemMessageType.JoinLobbyFinished, StatusCode.Success, 0, session.roomKeyID, 1, room.ID); 
            response = new(fPacket.channel, (byte)FClientOpcode.SystemMessage, buffer);
            await room.Host.Send(await response.ToSend());
        }
    }
}