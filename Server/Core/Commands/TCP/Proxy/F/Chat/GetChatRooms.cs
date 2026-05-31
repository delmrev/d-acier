public class GetChatRooms
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        // Income payload int32 gameid
        var data = await Reader.ReadBytes(fPacket.payload,"I");
        var chats = Global.GetChats((int)data[0]);
        if(chats.Count == 0)
        {
            Global.Add_Chat("global",session.game_id);
        }
        FResponse response = new(fPacket.channel, FClientOpcode.NETWORK_CHANNEL_CHAT_NBROOMS, await Writer.WriteBytes("II", (int)data[0], chats.Count == 0 ? 1 : chats.Count));
        await ProxyReader.FinalizePacket(await response.ToSend(), session);
        foreach (var option in chats)
        {
            var buffer = await Writer.WriteBytes("Is", Global.GetChat(option.Key,(int)data[0]).users.Count, option.Key);
            response = new(fPacket.channel, FClientOpcode.BM_CHAT_ROOM_INFO, buffer);
            await ProxyReader.FinalizePacket(await response.ToSend(), session);
        }
    }
}