public class FPacket
{
    public byte Opcode;
    public int channel;
    public ushort PayloadLength;
    public byte fOpcode;
    public byte[] payload;
    public FPacket(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        using BinaryReader reader = new(stream);
        Opcode = reader.ReadByte();
        channel = Reader.ReadInt32Be(reader);
        PayloadLength = Reader.ReadInt16Be(reader);
        fOpcode = reader.ReadByte();
        payload = reader.ReadBytes(PayloadLength-1);
    }
    public FPacket(int channel,byte opcode, List<byte> payload)
    {
        fOpcode = opcode;
        this.channel = channel;
        this.payload = [..payload];
        PayloadLength = (ushort)payload.Count;
        PayloadLength++;
    }
    public async Task<List<byte>> ToSend()
    {
        var buffer = await Writer.WriteBytes("BIHB",
        0x66,
        channel,
        PayloadLength,
        fOpcode
        );
        buffer.AddRange(payload);
        buffer.InsertRange(0,await Writer.WriteBytes("H",buffer.Count));
        return buffer;
    }
}