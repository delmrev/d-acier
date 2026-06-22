using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class GetRoomList : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            session.has_EF = true;
            var buffer = await Writer.WriteBytes("H", 0);
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.Brausing, buffer);
            await session.Send(await response.ToSend()); 
            buffer = await Writer.WriteBytes("LLLLLLLLLLLLLL", await GlobalManager.GetPlayersCount(session.game_id), await GlobalManager.GetRoomsCount(session.game_id), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            response = new(fPacket.channel, (byte)FClientOpcode.PublicInformation, buffer);
            await session.Send(await response.ToSend()); 
            response = new(fPacket.channel, (byte)FClientOpcode.BrausingMessageEnd, [0x00, 0x01]);
            await session.Send(await response.ToSend());
        }
    }
}