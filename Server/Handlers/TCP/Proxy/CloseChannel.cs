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
            if((int)read[1] == 0)
            {
                session.channels.Remove("mms");
                foreach(var value in session.channels)
                {
                    GPacket gPacket = new(value.Value);
                    await session.Send(await gPacket.ToSend());
                }
                session.channels.Clear();
            } else
            {
                try
                {
                    var channel = session.channels.FirstOrDefault(r => r.Value == (int)read[1]);
                    session.channels.Remove(channel.Key);
                    Log.Info($"Session: {session.EugenID} closed channel: {channel.Key}"); 
                } catch {}
            }
        }
    }
}