public enum PacketType : byte
{
    REDDRAGONLOGIN = 0x41,
    CONNECT = 0x42, // B
    NORMANDYCONNECT = 0x62,
    CONNECT_SERVER = 0x63, //c
    CONFIRM = 0x64, // d
    DATA = 0x66, // f
    CLOSE_CHANNEL = 0x67, // g
    EuropeanEscalationLogin = 0xE7
}