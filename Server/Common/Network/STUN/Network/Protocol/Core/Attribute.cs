public enum Attribute : ushort
{
    MappedAddress = 0x0001,
    ChangeRequest = 0x0003,
    SourceAddress = 0x0004,
    ChangedAddress = 0x0005,
    Username = 0x0006,
    Password = 0x0007,
    MessageIntegrity = 0x0008,
    ErrorCode = 0x0009,
    UnkwnownAttribute = 0x000a,
    ReflectedFrom = 0x000b,
    XorMappedAddress = 0x0020,
    Software = 0x8022,
}