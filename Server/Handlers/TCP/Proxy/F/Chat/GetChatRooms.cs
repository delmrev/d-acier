using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class GetChatRooms : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            var chats = await ChatManager.Instance.GetChats(session.game_id);
            if(chats.IsEmpty)
            {
                await ChatManager.Instance.Add_Chat("global",session.game_id);
            }
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.NETWORK_CHANNEL_CHAT_NBROOMS, Writer.WriteBytes("II", session.game_id, chats.IsEmpty ? 1 : chats.Count));
            await session.Send(response.ToBytes());
            foreach (var option in chats)
            {
                Chat chat = await ChatManager.Instance.GetChat(option.Key,session.game_id);
                var buffer = Writer.WriteBytes("Is", chat.users.Count, option.Key);
                response = new(fPacket.channel, (byte)FClientOpcode.BM_CHAT_ROOM_INFO, buffer);
                await session.Send(response.ToBytes());
            }
        }
    }
}