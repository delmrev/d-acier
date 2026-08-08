using System.Text.Json;
using NLog;

public class AutomatchManager
{
    public ConfigData config;
    public static AutomatchManager Instance => _instance;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly AutomatchManager _instance = new();
    private static readonly Dictionary<int,List<Session>> automatch_list = new();
    private readonly object locker = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
    public async Task AddToAutoMatch(Session session) {
        lock (locker)
        {
            automatch_list.TryAdd(session.game_id,new());
            automatch_list[session.game_id].Add(session);
            if(automatch_list[session.game_id].Count >= 2)
            {
                var host = automatch_list[session.game_id][0];
                var user = automatch_list[session.game_id][1];
                _ = Task.Run(async() => StartAutomatch(host,user));
                automatch_list[session.game_id].Remove(automatch_list[session.game_id][1]);
                automatch_list[session.game_id].Remove(automatch_list[session.game_id][0]);
            } else
            {
                Log.Debug($"{automatch_list[session.game_id].Count}");
            }
        }
    }
    private async Task StartAutomatch(Session host, Session user)
    {
        if(host.currentRoom != null)
        {
            await LobbyManager.Instance.RemoveRoom(host.currentRoom.ID,host.game_id);
            host.currentRoom.Dispose();
            host.currentRoom = null;
        }
        if(user.currentRoom != null)
        {
            await LobbyManager.Instance.RemoveRoom(user.currentRoom.ID,host.game_id);
            user.currentRoom.Dispose();
            user.currentRoom = null;
        }
        Lobby new_lobby = new(host, await LobbyManager.Instance.GetRoomID());
        new_lobby.Users.Add(2,host);
        new_lobby.Users.Add(4,user);
        host.roomKeyID = 2;
        user.roomKeyID = 4;
        host.currentRoom = new_lobby;
        user.currentRoom = new_lobby;
        var buffer = Writer.WriteBytes("LLLLLLLLLLLLLL", await GlobalManager.Instance.GetPlayersCount(host.game_id), 0,2, 389, 255, 2, 0, 2, 0, 0, 0, 0, 0, 0);
        FPacket response = new(1, (byte)FClientOpcode.PublicInformation, buffer);
        await host.Send(response.ToBytes()); 
        await user.Send(response.ToBytes()); 
        //host
        var confPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Configuration/Automatch",
                $"{host.game_id}.json"
        );
        string jsonText = File.ReadAllText(confPath);
        string[] maps = JsonSerializer.Deserialize<string[]>(jsonText, JsonOptions) ?? [];
        string randomMap = maps[Random.Shared.Next(0, maps.Length)];
        string map = JsonSerializer.Serialize(new { scenario = randomMap });
        buffer = Writer.WriteBytes("aQaLBBBs", true, new_lobby.ID, true, 0,0x2, 0x1, 0x0, map);
        response = new(1,(byte)FClientOpcode.AutoMatchCreated,buffer);
        await host.Send(response.ToBytes());
        buffer = Writer.WriteBytes("BBHLLQs", 0x66, 0x00, 0, 1233002674,-1872129687, new_lobby.ID, "Relay.1"); // 1233002674 - 178.32.126.73
        response = new(1, (byte)FClientOpcode.SystemMessage_2, buffer);
        await host.Send(response.ToBytes());
        buffer = Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.MessageHostChanged, StatusCode.Success, 46074, 2, -1, new_lobby.ID);
        response = new(1, (byte)FClientOpcode.LobbyMessage, buffer);
        await host.Send(response.ToBytes());
        buffer = Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.LobbyEnterFinished, StatusCode.Success, 46074, host.roomKeyID, -1, new_lobby.ID);
        response = new(1, (byte)FClientOpcode.LobbyMessage, buffer);
        await host.Send(response.ToBytes());
        buffer = Writer.WriteBytes("BBHQLQ", LobbyCommandsClient.Connect, StatusCode.Success, 0, new_lobby.ID, user.roomKeyID, user.EugenID);
        response = new(1, (byte)FClientOpcode.LobbyMessage, buffer);
        await host.Send(response.ToBytes());
        response = new(1, (byte)FClientOpcode.Unknown, [0x0,0x0,0x0,0x0]);
        await host.Send(response.ToBytes());
        // user
        buffer = Writer.WriteBytes("aQaLBBBs", true, new_lobby.ID, false, 1,0x2, 0x1, 0x0, map); 
        response = new(1,(byte)FClientOpcode.AutoMatchCreated,buffer);
        await user.Send(response.ToBytes());
        buffer = Writer.WriteBytes("BBHQLQ", LobbyCommandsClient.Connect, StatusCode.Success, 0, new_lobby.ID, new_lobby.Host.roomKeyID, new_lobby.Host.EugenID);
        response = new(1, (byte)FClientOpcode.LobbyMessage, buffer);
        await user.Send(response.ToBytes());
        buffer = Writer.WriteBytes("BBHLLQs", 0x66, 0x00, 0, 1233002674,-1872129687, new_lobby.ID, "Relay.1"); // 1233002674 - 178.32.126.73
        response = new(1, (byte)FClientOpcode.SystemMessage_2, buffer);
        await user.Send(response.ToBytes());
        buffer = Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.MessageHostChanged, StatusCode.Success, 46074, 2, -1, new_lobby.ID);
        response = new(1, (byte)FClientOpcode.LobbyMessage, buffer);
        await user.Send(response.ToBytes());
        buffer = Writer.WriteBytes("BBHLLQ", LobbyCommandsClient.LobbyEnterFinished, StatusCode.Success, 46074, user.roomKeyID, -1, new_lobby.ID);
        response = new(1, (byte)FClientOpcode.LobbyMessage, buffer);
        await user.Send(response.ToBytes());
        response = new(1, (byte)FClientOpcode.Unknown, [0x0,0x0,0x0,0x0]);
        await user.Send(response.ToBytes());
        buffer = Writer.WriteBytes("BBHLLQ", SystemMessageType.JoinLobbyFinished, StatusCode.Success, 0, 2, 1, new_lobby.ID); 
        response = new(1, (byte)FClientOpcode.SystemMessage, buffer);
        await user.Send(response.ToBytes());
        buffer = Writer.WriteBytes("BBHLLQ", SystemMessageType.JoinLobbyFinished, StatusCode.Success, 0, 4, 1, new_lobby.ID); 
        response = new(1, (byte)FClientOpcode.SystemMessage, buffer);
        await host.Send(response.ToBytes());
    }
    public async Task RemoveFromAutomatch(Session session)
    {
        automatch_list[session.game_id].Remove(session);
    }
    public async Task<List<Session>> GetList(int gameid){
        automatch_list.TryAdd(gameid,new());
        return automatch_list[gameid];
    }
    public async Task<int> GetAutomatchPlayerCount()
    {
        return automatch_list.Count;
    }
}