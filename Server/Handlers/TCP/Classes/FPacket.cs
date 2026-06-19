public class FPacket
{
    public byte Opcode;
    public int channel;
    public ushort PayloadLength;
    public FServerOpcode fOpcode;
    public byte[] payload;
    public FPacket(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        using BinaryReader reader = new(stream);
        Opcode = reader.ReadByte();
        channel = Reader.ReadInt32Be(reader);
        PayloadLength = Reader.ReadInt16Be(reader);
        fOpcode = (FServerOpcode)reader.ReadByte();
        payload = reader.ReadBytes(PayloadLength-1);
    }
    public async Task<List<byte>> ToSend()
    {
        var buffer = await Writer.WriteBytes("BIHB",
        (byte)'f',
        channel,
        PayloadLength,
        fOpcode
        );
        buffer.AddRange(payload);
        return buffer;
    }
}