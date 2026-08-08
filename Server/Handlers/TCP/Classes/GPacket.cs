public class GPacket
{
    public byte Opcode;
    public int channel;
    public GPacket(byte[] bytes)
    {
        var values = Reader.ReadBytes(bytes,"BI");
        Opcode = (byte)values[0];
        channel = (int)values[1];
    }
    public GPacket(int channel)
    {
        this.channel = channel;
        Opcode = 0x67;
    }
    public byte[] ToBytes()
    {
        return Writer.WriteBytes("HBI",5,Opcode,channel);
    }
}