using System.Buffers;
using System.Buffers.Binary;

public class PacketAttribute
{
    public Attribute Type;
    public byte[] Value;
    public PacketAttribute(ushort type, byte[] value)
    {
        Type = (Attribute)type;
        Value = value;
    }
    public PacketAttribute(){}
}