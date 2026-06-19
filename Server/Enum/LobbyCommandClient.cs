public enum LobbyCommandsClient : byte
{
    MESSAGE_HOST_CHANGED = 0x68, // h
    LOBBY_ENTER_FINISHED = 0x6A, // j
    DISCONNECT = 0x6C, // l
    UNK_DISCONNECT = 0x6B, // k 
    KICK = 0x67,  // g
    UNK2_DISCONNECT = 0x6F, // o
    CONNECT = 0x63 // c
}