namespace EugnetProtocol.Common.Interfaces
{
    public interface IFPacketHandler
    {
        public Task Process(FPacket  packet, Session session);
    }
}
