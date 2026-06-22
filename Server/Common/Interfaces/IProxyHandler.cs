namespace EugnetProtocol.Common.Interfaces
{
    public interface IProxyHandler
    {
        public Task Process(byte[] data, Session session);
    }
}