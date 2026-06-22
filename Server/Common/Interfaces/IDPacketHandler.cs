namespace EugnetProtocol.Common.Interfaces
{
    public interface IDPacketHandler
    {
        public Task Process(DPacket packet, Session session);
    }
}
