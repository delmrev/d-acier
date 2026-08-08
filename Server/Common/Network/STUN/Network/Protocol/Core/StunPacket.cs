using System.Buffers;
using System.Buffers.Binary;

public class StunPacket
{
    public MessageType Type;
    public byte[] TransactionID;
    public List<PacketAttribute> Attributes;
    public StunPacket(byte[] raw)
    {
        if(raw.Length < 20)
        {
            throw new ArgumentOutOfRangeException("Invalid packet");
        }
        Attributes = new();
        ReadOnlySpan<byte> buffer = raw;
        Type = (MessageType)BinaryPrimitives.ReadUInt16BigEndian(buffer);
        ushort length = BinaryPrimitives.ReadUInt16BigEndian(buffer[2..]);
        if(raw.Length < length + 20)
        {
            throw new ArgumentOutOfRangeException("Invalid packet");
        }
        TransactionID = buffer[8..20].ToArray();
        buffer = buffer.Slice(20, length);
        while (buffer.Length >= 4)
        {
            PacketAttribute attribute = new()
            {
                Type = (Attribute)BinaryPrimitives.ReadUInt16BigEndian(buffer)
            };
            length = BinaryPrimitives.ReadUInt16BigEndian(buffer[2..]);
            attribute.Value = buffer.Slice(4, length).ToArray();
            Attributes.Add(attribute);
            int paddedLength = (length + 3) & ~3;
            if (buffer.Length < 4 + paddedLength)
            {
                break;
            }
            buffer = buffer[(4 + paddedLength)..];
        }
        
    }
    public StunPacket(MessageType type, byte[] transactionID)
    {
        Type = type;
        TransactionID = transactionID;
        Attributes = new();
    }
    public byte[] ToBytes()
    {
        ushort totalAttributesLength = 0;
        foreach (var attr in Attributes)
        {
            int length = attr.Value.Length;
            int padding = (4 - (length % 4)) % 4;
            totalAttributesLength += (ushort)(4+length+padding);
        }
        int totalPacketLength = 20 + totalAttributesLength;
        var writer = new ArrayBufferWriter<byte>(totalPacketLength);
        Span<byte> span = writer.GetSpan(totalPacketLength);
        BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)Type);
        BinaryPrimitives.WriteUInt16BigEndian(span[2..], totalAttributesLength);
        BinaryPrimitives.WriteUInt32BigEndian(span[4..], 0x2112A442);
        TransactionID.AsSpan(0, 12).CopyTo(span[8..20]);
        int offset = 20;
        foreach (var attr in Attributes)
        {
            ushort attrLength = (ushort)attr.Value.Length;
            BinaryPrimitives.WriteUInt16BigEndian(span[offset..], (ushort)attr.Type);
            BinaryPrimitives.WriteUInt16BigEndian(span[(offset + 2)..], attrLength);
            offset += 4;
            attr.Value.AsSpan().CopyTo(span[offset..]);
            offset += attrLength;
            int padding = (4 - (attrLength % 4)) % 4;
            if (padding > 0)
            {
                span.Slice(offset, padding).Clear();
                offset += padding;
            }
        }
        writer.Advance(totalPacketLength);
        return writer.WrittenSpan.ToArray();
    }
}