using Database.Tables;
using NLog;

public static class Global
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static Dictionary<long,Room> Rooms = new();
    private static Dictionary<long,Session> Players = new();
    private static object locker = new();
    private static object chatLocker = new();
    private static object roomLocker = new();
    private static object configDataLocker = new();
    private static object automatch = new();
    private static readonly Dictionary<int, Dictionary<string, Chat>> GameChatList = new();
    private static List<Session> automatch_list = new();
    private static ConfigData? Confdata;
    public static void AddRoom(Room room)
    {
        lock (roomLocker)
        {
            Rooms.Add(room.ID,room);
        }
    }
    public static void RemoveRoomFromList(Room room)
    {
        lock (roomLocker)
        {
            Rooms.Remove(room.ID);
        }
    }
    public static void RegSession(Session session)
    {
        lock (locker)
        {
            Players.Add(session.EugenID,session);
        }
    }
    public static void LogOutSession(Session session)
    {
        lock (locker)
        {
            Players.Remove(session.EugenID);
        }
    }
    public static int GetPlayersCount()
    {
        lock (locker)
        {
            return Players.Count;
        }
    }
    public static int GetRoomsCount()
    {
        lock (roomLocker)
        {
            return Rooms.Count;
        }
    }
    public static Dictionary<long,Room> GetRoomList()
    {
        lock (roomLocker)
        {
            return Rooms;
        }
    }
    public static Session? GetSession(long EugenID)
    {
        lock (locker)
        {
            return Players[EugenID];
        }
    }
    public static Room GetRoom(long roomID)
    {
        lock (roomLocker)
        {
            return Rooms[roomID];
        }
    }
    public static void RemoveRoom(long roomID)
    {
        Rooms.Remove(roomID);
    }
    public static Dictionary<string, Chat> GetChats(int gameid)
    {
        if (!GameChatList.ContainsKey(gameid))
        {
            GameChatList.Add(gameid,new());
        }
        return GameChatList[gameid];
    }
    public static void JoinChat(string chatKey,int gameid, Session session)
    {
        lock (chatLocker)
        {
            if (!GameChatList.ContainsKey(gameid))
            {
                GameChatList.Add(gameid,new());
            }
            var ChatList = GameChatList[gameid];
            if (!ChatList.ContainsKey(chatKey))
            {
                Chat newChat = new();
                newChat.users.Add(session);
                ChatList.Add(chatKey,newChat);
            } else {
                var chat = ChatList[chatKey];
                chat.users.Add(session);
            }
        }
    }
    public static void LeftChat(string chatKey, Session session, int gameid)
    {
        lock (chatLocker)
        {
            if (!GameChatList.ContainsKey(gameid))
            {
                GameChatList.Add(gameid,new());
            }
            var ChatList = GameChatList[gameid];
            if (!ChatList.ContainsKey(chatKey))
            {
                Log.Error($"Try to disconect non-existent chat: {chatKey}");
            } else {
                var chat = ChatList[chatKey];
                chat.users.Remove(session);
            }
        }
    }
    public static void SendMessage(FResponse responce, string chatKey, int gameid)
    {
        lock (chatLocker)
        {
            if (!GameChatList.ContainsKey(gameid))
            {
                GameChatList.Add(gameid,new());
            }
            var ChatList = GameChatList[gameid];
            if (!ChatList.ContainsKey(chatKey))
            {
                Log.Error($"Try to send message in non-existent chat: {chatKey}");
            } else {
                var chat = ChatList[chatKey];
                for(int i = 0; i < chat.users.Count; i++)
                {
                    Task.Run(() => ProxyReader.FinalizePacket(responce.ToSend(),chat.users[i]));
                }
            }
        }
    }
    public static void SetConfigData(ConfigData data)
    {
        lock (configDataLocker)
        {
           Confdata = data; 
        }
    }
    public static ConfigData? GetConfigData()
    {
        lock (configDataLocker)
        {
           return Confdata;
        }
    }
    public static void Add_Chat(string chatKey, int gameid)
    {
        lock (chatLocker)
        {
            if (!GameChatList.ContainsKey(gameid))
            {
                GameChatList.Add(gameid,new());
            }
            if (GameChatList[gameid].ContainsKey(chatKey))
            {
                return;
            } else
            {
                GameChatList[gameid].Add(chatKey,new());
            }
        }
    }
    public static Chat GetChat(string chatKey, int gameid)
    {
        lock (chatLocker)
        {
            if (!GameChatList.ContainsKey(gameid))
            {
                GameChatList.Add(gameid,new());
            }
            var ChatList = GameChatList[gameid];
            if (!ChatList.ContainsKey(chatKey))
            {
                Chat newChat = new();
                ChatList.Add(chatKey,newChat);
                return newChat;
            } else {
                var chat = ChatList[chatKey];
                return chat;
            }
        }
    }
    public static async Task Stop()
    {
        lock (locker){
            for(int i = 0; i < Players.Count; i++)
            {
                for(int j = 0; j< Players[i].channels.Count; j++)
                {
                    var budder = Writer.WriteBytes("BI", PacketType.CLOSE_CHANNEL, Players[i].channels[i]);
                }
                Players[i].Dispose();
            }
            Log.Info("Server stopped. All users disconnected");
        }
    }
    public static async Task AddToAutoMatch(Session session) {
        lock (automatch) 
        {
            automatch_list.Add(session);
        }
    }

    public static async Task RemoveFromAutomatch(Session session)
    {
        lock (automatch)
        {
            automatch_list.Remove(session);
        }
    }
    public static async Task<Session[]> GetList() => [.. automatch_list];
}
