public enum FServerOpcode : byte
{
    UPDATE_STATS = 0x34,
    LobbyPrivateMessage = 0xEB,
    CONTINUE = 0xC1,
    FRIEND_COMMAND = 0xC2, // Client- BQ
    PING = 0xC3,
    Signal = 0xC4,
    CURRENT_STATE = 0xC5,
    CREATE_TEAM = 0xC6,
    FRIEND_TEAM_COMMAND = 0xC7, // 10 state (also Team_Command)
    BM_FRIEND_MESSAGE = 0xC8,
    FRIEND_GET_CHAT_ROOMS = 0xC9,
    CHAT_JOIN = 0xCA,
    CHAT_LEAVE = 0xCC,
    CHAT_MSG = 0xCD,
    SEND_EUGEN_ID = 0xD0,
    NETWORK_CHANNEL_GET_FRIENDS = 0xD4,
    CONNECT = 0xE1, //Steam Connect: QBQ ;Direct TCP connect: QBH#cH#c; //STUN_INFO: QBBLHLH#v;
    LOBBY_SYSTEM_MSG = 0xE2,
    PING_2 = 0xE3,
    unk_1 = 0xE5, // QQ
    unk = 0xE7, // I
    GET_PUBLIC_INFORMATION = 0xE8,
    LOBBY_MSG = 0xE9,
    LOBBY_INFO = 0xEA,
    SEND_LOBBY_INVITATION = 0xED, // QQa
    GET_ROOM_LIST = 0xEF,
    SEND_CHAT_MSG_LOBBY = 0xF1, // F1 - QQs#v
}