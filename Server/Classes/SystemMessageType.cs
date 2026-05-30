public enum SystemMessageType : byte
{
    ON_LOBBY_ENTERED = 0x61, // a
    ON_LOBBY_CREATED = 0x64, // d
    UNK = 0x66, //f  Log_string((volatile signed __int32 **)&v39, &pNodeName); ??
    DISCONNECT_FROM_MMS = 0x6D, // disconnection message
    JOIN_LOBBY_FINISHED = 0x6E // n, I came up with this name
}