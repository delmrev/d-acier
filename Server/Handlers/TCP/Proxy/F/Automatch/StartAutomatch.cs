using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class StartAutomatch : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            var data = Reader.ReadBytes(fPacket.payload,"BQBBBBQLBB");
            var buffer = Writer.WriteBytes("BBHLLQ",0,0,0,0,0,(long)data[6]);
            FPacket response = new(fPacket.channel,(byte)FClientOpcode.AutoMatchStart,buffer);
            await session.Send(response.ToBytes());
            await AutomatchManager.Instance.AddToAutoMatch(session);
        }
    }
}
