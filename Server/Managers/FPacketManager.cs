using EugnetProtocol.Common.Interfaces;
using EugnetProtocol.TCP.Proxy.F;
using NLog;

namespace EugnetProtocol.TCP.Proxy
{
    public class FPacketManager : IProxyHandler
    {
        private readonly Dictionary<byte, IFPacketHandler> _handlers = new();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly FriendCommand _friendCommand = new();
        private readonly UserData _userData = new();
        public FPacketManager()
        {
            _handlers.Add((byte)FServerOpcode.UPDATE_STATS, new UpdateStats());
            _handlers.Add((byte)FServerOpcode.LOBBY_INFO, new LobbySettings());
            _handlers.Add((byte)FServerOpcode.NETWORK_CHANNEL_GET_FRIENDS, new GetFriends());
            _handlers.Add((byte)FServerOpcode.FRIEND_GET_CHAT_ROOMS, new GetChatRooms());
            _handlers.Add((byte)FServerOpcode.SEND_EUGEN_ID, new SendEugenID());
            _handlers.Add((byte)FServerOpcode.CHAT_JOIN, new JoinChat());
            _handlers.Add((byte)FServerOpcode.CHAT_LEAVE, new LeaveChat());
            _handlers.Add((byte)FServerOpcode.CHAT_MSG, new ChatMessage());
            _handlers.Add((byte)FServerOpcode.LobbyPrivateMessage, new LobbyPrivateMSG());
            _handlers.Add((byte)FServerOpcode.Signal, new Signal());
            _handlers.Add((byte)FServerOpcode.BM_FRIEND_MESSAGE, new PrivateMessage());
            _handlers.Add((byte)FServerOpcode.GET_PUBLIC_INFORMATION, new GetPublicInformation());
            _handlers.Add((byte)FServerOpcode.GET_ROOM_LIST, new GetRoomList());
            _handlers.Add((byte)FServerOpcode.LOBBY_SYSTEM_MSG, new LobbySystemMsg());
            _handlers.Add((byte)FServerOpcode.CURRENT_STATE, new CurrentState());
            _handlers.Add((byte)FServerOpcode.SEND_CHAT_MSG_LOBBY, new LobbyMsg());
            _handlers.Add((byte)FServerOpcode.CONTINUE, new Continue());
            _handlers.Add((byte)FServerOpcode.CONNECT, new ConnectStartP2P());
            _handlers.Add((byte)FServerOpcode.LOBBY_MSG, new LobbyInfoMsg());
            var keepAliveHandler = new Keep_Alive();
            _handlers.Add((byte)FServerOpcode.KEEP_ALIVE_PACKET, keepAliveHandler);
            _handlers.Add((byte)FServerOpcode.KEEP_ALIVE_PACKET_2, keepAliveHandler);
            _handlers.Add((byte)FServerOpcode.FRIEND_COMMAND, new LambdaHandler(async (p, s) =>
            {
                if (p.channel == 2)
                {
                    await _friendCommand.Process(p, s);
                }
                else
                {
                    await  _userData.Process(p, s);
                }
            }));
        }
        public async Task Process(byte[] data, Session session)
        {
            FPacket packet = new(data);
            if(_handlers.TryGetValue(packet.fOpcode,out var handler))
            {
                await handler.Process(packet,session);
            } else
            {
                Log.Warn($"Unknown fopcode: {packet.fOpcode}");
            }
        }
    }
}