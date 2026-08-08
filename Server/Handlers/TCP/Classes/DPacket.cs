public class DPacket
{
    public byte Opcode;
    public int channel;
    public string command;
    public DPacket(byte[] bytes)
    {
        var values = Reader.ReadBytes(bytes, "BIS");
        Opcode = (byte)values[0];
        channel = (int)values[1];
        command = (string)values[2];
    }
}