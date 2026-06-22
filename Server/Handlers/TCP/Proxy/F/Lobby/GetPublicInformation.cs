using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class GetPublicInformation : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            object[] values;
            using MemoryStream ms = new(fPacket.payload);
            using BinaryReader reader = new(ms);
            var read = await Reader.ReadBytes(reader, "H");
            ushort searchid = (ushort)read[0];
            FPacket response;
            string page = "";
            for (int i = 0; i < 5; i++)
            {
                values = await Reader.ReadBytes(reader, "HLBHc");
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
                    var list = await GlobalManager.GetRoomList(session.game_id);
                    var buf = await Writer.WriteBytes("H", list.Count);
                    response = new(fPacket.channel,(byte)FClientOpcode.Brausing,buf);
                    await session.Send(await response.ToSend());
                    foreach(var room in list)
                    {
                        List<byte> pack = [];
                        buf = await Writer.WriteBytes("HQH", searchid, room.Key, 2);
                        pack.AddRange(buf);
                        int[] indexeses = [1, 0, 4, 3, 7, 8, 264, 256, 2, 265, 260, 259, 261, 258, 263, 268, 267, 262, 257];
                        for (int i = 0; i < indexeses.Length; i++)
                        {

                            try
                            {
                                var option = await Writer.WriteBytes("Ic",indexeses[i],room.Value.RoomSettings[indexeses[i]]);
                                var length = await Writer.WriteBytes("H",option.Count);
                                pack.AddRange(length);
                                pack.AddRange(option);
                            }
                            catch
                            {
                                continue;
                            }
                        }
                        response = new(fPacket.channel,(byte)FClientOpcode.Brausing,pack);
                        await session.Send(await response.ToSend());
                    }
                } else
                {
                    var buf = await Writer.WriteBytes("H", 0);
                    response = new(fPacket.channel, (byte)FClientOpcode.Brausing, buf);
                    await session.Send(await response.ToSend()); 
                }
                var buffer = await Writer.WriteBytes("LLLLLLLLLLLLLL", await GlobalManager.GetPlayersCount(session.game_id), await GlobalManager.GetRoomsCount(session.game_id), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                response = new(fPacket.channel, (byte)FClientOpcode.PublicInformation, buffer);
                await session.Send(await response.ToSend()); 
            }
        }
    }
}