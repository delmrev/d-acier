using EugnetProtocol.Common.Interfaces;
using NLog;
namespace EugnetProtocol.TCP.Proxy.F
{
    public class LobbySettings : IFPacketHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(FPacket fPacket, Session session)
        {
            object[] values;
            if(session.currentRoom is null)
            {
                Log.Error("Try to get current room but dont have current room");
                return;
            }
            values = Reader.ReadBytes(fPacket.payload, "QIBc");
            if((byte)values[2] == 0x01) // if flag, dont save
            {
                Log.Debug($"Dont save; id: {values[1]}, value: {values[3]}");
                return;
            }
            int id = (int)values[1];
            string value = (string)values[3];
            switch (id)
            {
                case 4:
                    session.currentRoom.Is_public = int.Parse((string)values[3]) == 0;
                break;
                case 7:
                    // is running?
                break;
            }
            if (!session.currentRoom.RoomSettings.TryAdd(id, value))
            {
                if(value == session.currentRoom.RoomSettings[id])
                {
                    return;
                }
                session.currentRoom.RoomSettings[id] = value;
            }
            var buf = Writer.WriteBytes("QIBc",session.currentRoom.ID,id,(byte)values[2],value);
            FPacket response = new(fPacket.channel,(byte)FClientOpcode.LobbyInfo,buf);
            foreach(var user in session.currentRoom.Users)
            {
                await user.Value.Send(response.ToBytes());
            }
        }
    }
}