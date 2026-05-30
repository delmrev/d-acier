public enum PacketType : byte
{
    CONNECT = 0x42, // B
    CONNECT_SERVER = 0x63, //c
    CONFIRM = 0x64, // d
    DATA = 0x66, // f
    CLOSE_CHANNEL = 0x67, // g
    EMERGENCY_DISCONNECT = 0x7A // z
}