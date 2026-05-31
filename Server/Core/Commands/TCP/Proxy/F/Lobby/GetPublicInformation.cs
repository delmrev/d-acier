public static class GetPublicInformation
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        object[] values;
        using MemoryStream ms = new(fPacket.payload);
        using BinaryReader reader = new(ms);
        var read = await Reader.ReadBytes(reader, "H");
        short searchid = (short)read[0];
        FResponse response;
        string page = "";
        for (int i = 0; i < 5; i++)
        {
            values = await Reader.ReadBytes(reader, "HLBHc");
            switch (i)
            {
                case 0:
                page = (string)values[4];
                break;
                case 1:
                    session.SpecialRoomID = (string)values[4];
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
            if(int.Parse(page) == 1){ // i dont know
                var list = Global.GetRoomList();
                var buf = await Writer.WriteBytes("H", list.Count);
                response = new(fPacket.channel,FClientOpcode.Brausing,buf);
                await ProxyReader.FinalizePacket(await response.ToSend(),session);
                foreach(var room in list)
                {
                    List<byte> pack = [];
                    buf = await Writer.WriteBytes("HQH", searchid, room.Key, 2);
                    pack.AddRange(buf);
                    int[] indexeses = [1, 0, 4, 3, 7, 8, 264, 256, 2, 265, 260, 259, 261, 258, 263, 268, 267, 262, 257];
                    for (int i = 0; i < indexeses.Length; i++)
                    {
                        var option = await Writer.WriteBytes("Ic",indexeses[i],room.Value.RoomSettings[indexeses[i]]);
                        var length = await Writer.WriteBytes("H",option.Count);
                        pack.AddRange(length);
                        pack.AddRange(option);
                    }
                    response = new(fPacket.channel,FClientOpcode.Brausing,pack);
                    await ProxyReader.FinalizePacket(await response.ToSend(),session);
                }
            } else
            {
                var buf = await Writer.WriteBytes("H", 0);
                response = new(fPacket.channel, FClientOpcode.Brausing, buf);
                await ProxyReader.FinalizePacket(await response.ToSend(), session); 
            }
            var buffer = await Writer.WriteBytes("LLLLLLLLLLLLLL", Global.GetPlayersCount(), Global.GetRoomsCount(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            response = new(fPacket.channel, FClientOpcode.PublicInformation, buffer);
            await ProxyReader.FinalizePacket(await response.ToSend(), session); 
        }
    }
}