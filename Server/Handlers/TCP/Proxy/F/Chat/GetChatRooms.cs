using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class GetChatRooms : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            // Income payload int32 gameid
            var data = await Reader.ReadBytes(fPacket.payload,"I");
            var chats = await GlobalManager.GetChats((int)data[0]);
            if(chats.IsEmpty)
            {
                await GlobalManager.Add_Chat("global",session.game_id);
            }
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.NETWORK_CHANNEL_CHAT_NBROOMS, await Writer.WriteBytes("II", (int)data[0], chats.IsEmpty ? 1 : chats.Count));
            await session.Send(await response.ToSend());
            foreach (var option in chats)
            {
                Chat chat = await GlobalManager.GetChat(option.Key,(int)data[0]);
                var buffer = await Writer.WriteBytes("Is", chat.users.Count, option.Key);
                response = new(fPacket.channel, (byte)FClientOpcode.BM_CHAT_ROOM_INFO, buffer);
                await session.Send(await response.ToSend());
            }
        }
    }
}