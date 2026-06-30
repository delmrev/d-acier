public class GPacket
{
    public byte Opcode;
    public int channel;
    public GPacket(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        using BinaryReader reader = new(stream);
        Opcode = reader.ReadByte();
        channel = Reader.ReadInt32Be(reader);
    }
    public GPacket(int channel)
    {
        this.channel = channel;
        Opcode = 0x67;
    }
    public async Task<List<byte>> ToSend()
    {
        var buffer = await Writer.WriteBytes("BI",
        Opcode,
        channel
        );
        buffer.InsertRange(0,await Writer.WriteBytes("H",buffer.Count));
        return buffer;
    }
}