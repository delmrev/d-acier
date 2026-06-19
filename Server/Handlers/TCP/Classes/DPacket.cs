public class DPacket
{
    public byte Opcode;
    public int channel;
    public string command;
    public DPacket(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        using BinaryReader reader = new(stream);
        Opcode = reader.ReadByte();
        channel = Reader.ReadInt32Be(reader);
        command = Reader.UnpackString(reader);
    }
}