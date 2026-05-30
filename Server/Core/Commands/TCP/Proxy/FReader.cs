using NLog;

public class FReader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task ProcessFPacket(FPacket fPacket, Session session)
    {
        try
        {
            switch (fPacket.fOpcode)
            {
                case FServerOpcode.UPDATE_STATS:
                    await UpdateStats.Process(fPacket,session);
                    break;
                case FServerOpcode.LOBBY_INFO:
                    Log.Info("Processing LOBBY_MSG");
                    await LobbySettings.Process(fPacket, session);
                    break;

                case FServerOpcode.NETWORK_CHANNEL_GET_FRIENDS:
                    Log.Info("Processing NETWORK_CHANNEL_GET_FRIENDS");
                    await GetFriends.Process(fPacket, session);
                    break;

                case FServerOpcode.FRIEND_GET_CHAT_ROOMS:
                    Log.Info("Processing FRIEND_GET_CHAT_ROOMS");
                    await GetChatRooms.Process(fPacket, session);
                    break;

                case FServerOpcode.FRIEND_COMMAND:
                    if(fPacket.channel == 2){
                        Log.Info("Processing FRIEND_COMMAND");
                        await FriendCommand.Process(fPacket, session);
                    } else
                    {
                        await UserData.Process(fPacket,session);
                    }
                    break;

                case FServerOpcode.SEND_EUGEN_ID:
                    Log.Info("Processing SEND_EUGEN_ID");
                    await SendEugenID.Process(fPacket, session);
                    break;

                case FServerOpcode.CHAT_JOIN:
                    Log.Info("Processing CHAT_JOIN");
                    await JoinChat.Process(fPacket, session);
                    break;
                case FServerOpcode.CHAT_LEAVE:
                    Log.Info("Processing CHAT_LEAVE");
                    await LeaveChat.Process(fPacket, session);
                    break;
                case FServerOpcode.CHAT_MSG:
                    Log.Info("Processing CHAT_MSG");
                    await ChatMessage.Process(fPacket, session);
                    break;
                case FServerOpcode.LobbyPrivateMessage:
                    await LobbyPrivateMSG.Process(fPacket,session);
                break;
                case FServerOpcode.Signal:
                    await Signal.Process(fPacket,session);
                break;
                case FServerOpcode.BM_FRIEND_MESSAGE:
                    Log.Info("Processing BM_FRIEND_MESSAGE");
                    await PrivateMessage.Process(fPacket);
                    break;
                case FServerOpcode.GET_PUBLIC_INFORMATION:
                    Log.Info("Processing GET_PUBLIC_INFORMATION");
                    await GetPublicInformation.Process(fPacket, session);
                    break;

                case FServerOpcode.GET_ROOM_LIST:
                    Log.Info("Processing GET_ROOM_LIST");
                    await GetRoomList.Process(fPacket, session);
                    break;

                case FServerOpcode.LOBBY_SYSTEM_MSG:
                    Log.Info("Processing LOBBY_SYSTEM_MSG");
                    await LobbySystemMsg.Process(fPacket,session);
                    break;
                case FServerOpcode.CURRENT_STATE:
                    await CurrentState.Process(fPacket,session);
                    break;
                case FServerOpcode.SEND_CHAT_MSG_LOBBY:
                    await LobbyMsg.Process(fPacket,session);
                break;
                case FServerOpcode.CONTINUE:
                    Log.Info("Processing CONTINUE");
                    await Continue.Process(fPacket, session);
                    break;
                case FServerOpcode.CONNECT:
                    await ConnectStartP2P.Process(fPacket,session);
                break;
                case FServerOpcode.PING:
                    Log.Debug("Processing PING (ignored)");
                    break;

                case FServerOpcode.PING_2:
                    Log.Debug("Processing PING_2 (ignored)");
                    break;

                default:
                    Log.Warn("Unknown FPacket opcode {0}, Payload: {1}",
                        fPacket.fOpcode,
                        fPacket.payload.Length > 0 ? BitConverter.ToString(fPacket.payload) : "empty"
                    );
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing FPacket Opcode {0}", fPacket.fOpcode);
        }
    }
}
