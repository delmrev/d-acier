public class FResponse(int channel,FClientOpcode opcode, List<byte> payload)
{
    public int channel = channel;
    public FClientOpcode opcode = opcode;
    public List<byte> payload = payload;
    public List<byte> ToSend()
    {
        var buffer = Writer.WriteBytes("BI",
        (byte)'f',
        channel
        );
        payload.Insert(0, (byte)opcode);
        payload.InsertRange(0, Writer.WriteBytes("H", payload.Count));
        buffer.AddRange(payload);
        return buffer;
    }
}