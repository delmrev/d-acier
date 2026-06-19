public enum LobbyCommandsClient : byte
{
    MessageHostChanged = 0x68, // h
    LobbyEnterFinished = 0x6A, // j
    Disconnect = 0x6C, // l
    UnknownDisconnect = 0x6B, // k 
    Kick = 0x67,  // g
    Kick_2= 0x6F, // o
    Connect = 0x63 // c
}