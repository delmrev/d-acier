using System.Buffers.Binary;

public class FPacket
{
    public byte Opcode;
    public int channel;
    public ushort PayloadLength;
    public byte fOpcode;
    public byte[] payload;
    public FPacket(byte[] bytes)
    {
        ReadOnlySpan<byte> span = bytes;
        Opcode = span[0];
        channel = BinaryPrimitives.ReadInt32BigEndian(span[1..5]);
        PayloadLength = BinaryPrimitives.ReadUInt16BigEndian(span[5..7]);
        fOpcode = span[7];
        int payloadSize = PayloadLength - 1;
        payload = span.Slice(8, payloadSize).ToArray();
    }
    public FPacket(int channel,byte opcode, byte[] payload)
    {
        fOpcode = opcode;
        this.channel = channel;
        this.payload = [..payload];
        PayloadLength = (ushort)payload.Length;
        PayloadLength++;
    }
    public byte[] ToBytes()
    {
        int size = 7 + PayloadLength;
        byte[] buffer = new byte[2 + size];
        Span<byte> span = buffer;
        BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)size);
        span = span[2..];
        span[0] = 0x66;
        BinaryPrimitives.WriteInt32BigEndian(span[1..5], channel);
        BinaryPrimitives.WriteUInt16BigEndian(span[5..7],PayloadLength);
        span[7] = fOpcode;
        span = span[8..];
        payload.CopyTo(span);
        return buffer;
    }
}