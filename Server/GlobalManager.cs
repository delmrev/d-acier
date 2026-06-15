using System.Collections.Concurrent;
using NLog;

public static class Global
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<long,Lobby>> Rooms = new();
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<long,Session>> Players = new();
    private static readonly Dictionary<int,List<Session>> automatch_list = new();
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, Chat>> GameChatList = new();
    private static object configDataLocker = new();
    private static ConfigData? Confdata;
    public static async Task AddRoom(Lobby room, int gameid)
    {
        Rooms.TryAdd(gameid, new());
        Rooms[gameid].TryAdd(room.ID,room);
    }
    public static async Task RemoveRoomFromList(Lobby room, int gameid)
    {
        Rooms[gameid].TryRemove(room.ID, out _);
    }
    public static async Task RegSession(Session session, int gameid)
    {
        Players.TryAdd(gameid, new());
        Players[gameid].TryAdd(session.EugenID,session);
    }
    public static async Task LogOutSession(Session session)
    {
        Players[session.game_id].TryRemove(session.EugenID, out _);
    }
    public static async Task<int> GetPlayersCount(int gameid)
    {
        Players.TryAdd(gameid, new());
        return Players[gameid].Count;
    }
    public static async Task<int> GetRoomsCount(int gameid)
    {
        Rooms.TryAdd(gameid, new());
        return Rooms[gameid].Count;
    }
    
    public static async Task<ConcurrentDictionary<long,Lobby>> GetRoomList(int gameid)
    {
        Rooms.TryAdd(gameid, new());
        return Rooms[gameid];
    }
    public static async Task<Session?> GetSession(long EugenID, int gameid)
    {
        Players.TryAdd(gameid, new());
        return Players[gameid].GetValueOrDefault(EugenID);
    }
    public static async Task<Lobby> GetRoom(long roomID, int gameid)
    {
        return Rooms[gameid][roomID];
    }
    public static async Task RemoveRoom(long roomID, int gameid)
    {
        Rooms[gameid].TryRemove(roomID, out _);
    }
    public static async Task<ConcurrentDictionary<string, Chat>> GetChats(int gameid)
    {
        GameChatList.TryAdd(gameid,new());
        return GameChatList[gameid];
    }
    public static async Task JoinChat(string chatKey,int gameid, Session session)
    {
        GameChatList.TryAdd(gameid,new());
        var ChatList = GameChatList[gameid];
        if (!ChatList.TryGetValue(chatKey, out Chat? chat))
        {
            Chat newChat = new();
            newChat.users.Add(session);
            ChatList.TryAdd(chatKey,newChat);
        } else {
            chat.users.Add(session);
        }
    }
    public static async Task LeftChat(string chatKey, Session session)
    {
        GameChatList.TryAdd(session.game_id,new());
        var ChatList = GameChatList[session.game_id];
        if (!ChatList.TryGetValue(chatKey, out Chat? chat))
        {
            Log.Error($"Try to disconect non-existent chat: {chatKey}");
        } else {
            chat.users.Remove(session);
        }
    }
    public static async Task SendMessage(FResponse response, string chatKey, int gameid)
    {
        GameChatList.TryAdd(gameid,new());
        var ChatList = GameChatList[gameid];
        if (!ChatList.TryGetValue(chatKey, out Chat? chat))
        {
            Log.Error($"Try to send message in non-existent chat: {chatKey}");
        } else {
            for (int i = 0; i < chat.users.Count; i++)
            {
                await ProxyReader.FinalizePacket(await response.ToSend(),chat.users[i]);
            }
        }
    }
    public static async Task Add_Chat(string chatKey, int gameid)
    {
        GameChatList.TryAdd(gameid,new());
        GameChatList[gameid].TryAdd(chatKey,new());
    }
    public static async Task<Chat> GetChat(string chatKey, int gameid)
    {
        GameChatList.TryAdd(gameid,new());
        var ChatList = GameChatList[gameid];
        if (!ChatList.TryGetValue(chatKey, out Chat? chat))
        {
            Chat newChat = new();
            ChatList.TryAdd(chatKey,newChat);
            return newChat;
        } else {
            return chat;
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
    public static async Task Stop()
    {
        foreach(var maps in Players){
            for(int i = 0; i < maps.Value.Count; i++)
            {
                foreach(var list in maps.Value){
                    for(int j = 0; j< list.Value.channels.Count; j++)
                    {
                        var budder = Writer.WriteBytes("BI", PacketType.CLOSE_CHANNEL, list.Value.channels[i]);
                    }
                    list.Value.Dispose();
                }
            }
            Log.Info("Server stopped. All users disconnected");
        }
    }
    public static async Task AddToAutoMatch(Session session) { // this section can be changed in future (when i add work automatch)
        automatch_list.TryAdd(session.game_id,new());
        automatch_list[session.game_id].Add(session);
    }

    public static async Task RemoveFromAutomatch(Session session)
    {
        automatch_list[session.game_id].Remove(session);
    }
    public static async Task<List<Session>> GetList(int gameid){
        automatch_list.TryAdd(gameid,new());
        return automatch_list[gameid];
    }
}
