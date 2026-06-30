using System.Collections.Concurrent;
using NLog;

public class ChatManager
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly ChatManager _instance = new();
    public static ChatManager Instance => _instance;
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, Chat>> GameChatList = new();
    public async Task<ConcurrentDictionary<string, Chat>> GetChats(int gameid)
    {
        GameChatList.TryAdd(gameid,new());
        return GameChatList[gameid];
    }
    public async Task JoinChat(string chatKey,int gameid, Session session)
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
    public async Task LeftChat(string chatKey, Session session)
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
    public async Task SendMessage(FPacket response, string chatKey, int gameid)
    {
        GameChatList.TryAdd(gameid,new());
        var ChatList = GameChatList[gameid];
        if (!ChatList.TryGetValue(chatKey, out Chat? chat))
        {
            Log.Error($"Try to send message in non-existent chat: {chatKey}");
        } else {
            for (int i = 0; i < chat.users.Count; i++)
            {
                await chat.users[i].Send(await response.ToSend());
            }
        }
    }
    public async Task Add_Chat(string chatKey, int gameid)
    {
        GameChatList.TryAdd(gameid,new());
        GameChatList[gameid].TryAdd(chatKey,new());
    }
    public async Task<Chat> GetChat(string chatKey, int gameid)
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
}