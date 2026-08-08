using EugnetProtocol.Common.Interfaces;
using NLog;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class GetPublicInformation : IFPacketHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(FPacket fPacket, Session session)
        {
            object[] values;
            ReadOnlySpan<byte> span = fPacket.payload.AsSpan();
            var read = Reader.ReadBytes(ref span, "H");
            ushort searchid = (ushort)read[0];
            FPacket response;
            string page = "";
            for (int i = 0; i < 5; i++)
            {
                values = Reader.ReadBytes(ref span, "HLBHc");
                switch (i)
                {
                    case 0:
                    page = (string)values[4];
                    break;
                    default:
                        break;
                }
            }
            await Task.Delay(100);
            if (session.has_EF)
            {
                session.has_EF = false;
                return;
            } else
            {
                if(int.Parse(page) == 1){
                    var list = await LobbyManager.Instance.GetRoomList(session.game_id);
                    var new_list = list.Where(r => r.Value.Is_public).ToList();
                    var buf = Writer.WriteBytes("H", new_list.Count);
                    response = new(fPacket.channel,(byte)FClientOpcode.Brausing,buf);
                    await session.Send(response.ToBytes());
                    foreach(var room in new_list)
                    {
                        List<byte> pack = [];
                        buf = Writer.WriteBytes("HQH", searchid, room.Key, 2);
                        pack.AddRange(buf);
                        int[] indexeses = [1, 0, 4, 3, 7, 8, 264, 256, 2, 265, 260, 259, 261, 258, 263, 268, 267, 262, 257];
                        for (int i = 0; i < indexeses.Length; i++)
                        {
                            try
                            {
                                var option = Writer.WriteBytes("Ic",indexeses[i],room.Value.RoomSettings[indexeses[i]]);
                                var length = Writer.WriteBytes("H", option.Length);
                                pack.AddRange(length);
                                pack.AddRange(option);
                            }
                            catch(Exception ex)
                            {
                                Log.Debug(ex);
                                continue;
                            }
                        }
                        response = new(fPacket.channel,(byte)FClientOpcode.Brausing, [.. pack]);
                        await session.Send(response.ToBytes());
                    }
                } else
                {
                    var buf = Writer.WriteBytes("H", 0);
                    response = new(fPacket.channel, (byte)FClientOpcode.Brausing, buf);
                    await session.Send(response.ToBytes()); 
                }
                var buffer = Writer.WriteBytes("LLLLLLLLLLLLLL", await GlobalManager.Instance.GetPlayersCount(session.game_id), 0,await AutomatchManager.Instance.GetAutomatchPlayerCount(), 389, 255, await LobbyManager.Instance.GetRoomsCount(session.game_id), 0, await AutomatchManager.Instance.GetAutomatchPlayerCount(), 0, 0, 0, 0, 0, 0);
                response = new(fPacket.channel, (byte)FClientOpcode.PublicInformation, buffer);
                await session.Send(response.ToBytes()); 
            }
        }
    }
}