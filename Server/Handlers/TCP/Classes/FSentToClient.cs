public enum FClientOpcode : byte
{
    Stats = 0x31, // Server msg: QQsQQL && unknown, if #vs break
    Stats_2 = 0x32, // same
    Stats_3 = 0x33, // same
    Stats_4 = 0x34, // same 
    StatsResult = 0x35, //Server msg: aa
    AutoMatchStart = 0x42, // Server msg:BBHLLQ 
    AutoMatchCreated = 0x43, //Server msg: aQaLBBBs
    Unknown = 0x44,
    AutoMatchCancel = 0x45,//Server msg: BBHLLQ
    Connect = 0xE1, // unknown
    SystemMessage = 0xE2, //Server msg: BBHLLQ
    SystemMessage_2 = 0xF4, // Server msg: BBHLLQs
    FriendMessage = 0xE4,//Server msg: BLHHL
    IDMessage = 0x7A, //Server msg: Q#c
    Invite = 0xE5, //Server msg: QQ
    MMS_MSG_INIT = 0xE7,
    Brausing = 0xE8, //Server msg: TMmsStubPrivate::OnSessionReceived()
    LobbyMessage = 0xE9, // Server msg: TMmsStubPrivate::OnLobbyMsgReceived()
    LobbyInfo = 0xEA, // Server msg: QLB#c
    LobbyPrivateMessage = 0xEB, //Server msg: BBHLLQ
    PublicInformation = 0xEC, //Server msg: LLLLLLLLLLLLLL
    InviteResponse = 0xED, //Server msg: QQa
    BrausingMessageEnd = 0xEE, //Server msg: H
    BrausingMessage = 0xEF, // Server msg: unknown
    PublicInformationAlt = 0xF1, // Server msg: QQs#v
    GetLocale = 0xF2, //Client msg: s
    NETWORK_CHANNEL_DEDICATED_CLIENT_DATA = 0xC2, // unknown
    //NETWORK_CHANNEL_DEDICATED_SYSTEM_MSG = 0xC5, // unknown
    NETWORK_CHANNEL_DEDICATED_DISCONNECT = 0xC7, // unknown
    NETWORK_CHANNEL_DEDICATED_PLAYER_EVENT = 0xC8, // (as Player) Server msg: BQL
    //NETWORK_CHANNEL_DEDICATED_UPDATE_CLIENT_GAME_PROPERTY = 0xC9, // Server msg: ss
    NETWORK_CHANNEL_DEDICATED_UPDATE_CLIENT_USER_PROPERTY = 0xCA, // Server msg: if(uknown&&ss)
    NETWORK_CHANNEL_DEDICATED_GAME_STARTED = 0xCC, // Server msg: if(unk&&unk) while(unk)
    NETWORK_CHANNEL_DEDICATED_STATE_WILL_CHANGE = 0xCD, // Server msg: LLL
    NETWORK_CHANNEL_DEDICATED_STATE_CHANGED = 0xCE, // Server msg: LLL
    //NETWORK_CHANNEL_DEDICATED_UPDATE_PING_VALUE = 0xD0, // Server msg: QL

    // Friend command packets
    CONTINUE = 0xC1,
    
    BM_FRIEND_COMMAND = 0xC2,          //Server msg: BQ
    BM_FRIEND_UNKNOWN_C3 = 0xC3,       // Server msg: no responce
    BM_FRIEND_CONNECT_STATUS = 0xC4,   // Server msg: unknown
    BM_FRIEND_PRESENCE = 0xC5,          // Server msg: if(unk&&unk) while(unk)

    BM_TEAM_CREATE = 0xC6,              // 
    // Team system packets
    BM_TEAM_COMMAND = 0xC7,            // Server msg: BBIQQI
    
    // Chat system packets
    BM_FRIEND_CHAT = 0xC8,             // Server msg: Qs
    BM_FRIEND_REQUEST = 0xC9,
    BM_CHAT_JOIN = 0xCA,               // Server msg: Qs
    BM_CHAT_SYSTEM = 0xCB,             // Server msg: ss
    BM_CHAT_LEAVE = 0xCC,              // Server msg: Qs
    BM_CHAT_MESSAGE = 0xCD,            // Server msg: Qsss
    NETWORK_CHANNEL_CHAT_NBROOMS = 0xCE,            // Server msg: II
    BM_CHAT_ROOM_INFO = 0xCF,          // Server msg: Is
    
    // External API integration
    NETWORK_CHANNEL_FRIEND_ADD_EXTERNAL_API_FRIEND = 0xD4,     // Server msg: if(H) return else while aQBS
    BM_FRIEND_GET_EXTERNAL_ID = 0xD5,  // Server msg: aQBS
    BM_FRIEND_GET_EUGNET_ID = 0xD6,    // Server msg: if(H) return else while aQBS
    BM_FRIEND_GET_AUTH_TOKEN = 0xD7,    // byte ???
}
