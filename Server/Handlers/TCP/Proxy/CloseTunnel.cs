using EugnetProtocol.Common.Interfaces;
using NLog;

namespace EugnetProtocol.TCP.Proxy
{
    public class CloseChannel : IProxyHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(byte[] data, Session session)
        {
            var read = await Reader.ReadBytes(data,"BI");
            try
            {
                session.channels.Remove((int)read[1]);
            }
            catch
            {
                Log.Error("Try to remove non existing channel!");
            } finally
            {
                if(session.channels.Count == 0)
                {
                    session.Dispose();
                }
            }
        }
    }
}