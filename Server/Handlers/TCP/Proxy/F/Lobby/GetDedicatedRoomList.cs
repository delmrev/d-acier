using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class GetDedicatedRoomList : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            session.has_EF = true;
            var buffer = Writer.WriteBytes("H", 0);
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.Brausing, buffer);
            await session.Send(response.ToBytes()); 
            buffer = Writer.WriteBytes("LLLLLLLLLLLLLL", await GlobalManager.Instance.GetPlayersCount(session.game_id), 0,await AutomatchManager.Instance.GetAutomatchPlayerCount(), 389, 255, await LobbyManager.Instance.GetRoomsCount(session.game_id), 0, await AutomatchManager.Instance.GetAutomatchPlayerCount(), 0, 0, 0, 0, 0, 0);
            response = new(fPacket.channel, (byte)FClientOpcode.PublicInformation, buffer);
            await session.Send(response.ToBytes()); 
            response = new(fPacket.channel, (byte)FClientOpcode.BrausingMessageEnd, [0x00, 0x01]);
            await session.Send(response.ToBytes());
        }
    }
}