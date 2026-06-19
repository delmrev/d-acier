public enum SystemMessageType : byte
{
    OnLobbyEntered = 0x61,          // 'a'
    OnLobbyCreated = 0x64,          // 'd'
    
    Unknown = 0x66, // f  Log_string((volatile signed __int32 **)&v39, &pNodeName); ??
    
    DisconnectFromMms = 0x6D,     // disconnection message
    JoinLobbyFinished = 0x6E      // n, I came up with this name
}