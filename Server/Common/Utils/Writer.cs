using System.Buffers.Binary;
using System.Text;
using NLog;

public class Writer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static byte[] WriteBytes(string command, params object[] value)
    {
        if (command.Length != value.Length)
        {
            Log.Warn("Command length {0} does not match value length {1}", command.Length, value.Length);
        }
        
        int totalSize = 0;

        for (int i = 0; i < command.Length; i++)
        {
            switch (command[i])
            {
                case 'Q': 
                case 'q': 
                    totalSize += 8; 
                    break;
                case 'I': 
                case 'L': 
                    totalSize += 4; 
                    break;
                case 'H': 
                case 'h': 
                    totalSize += 2; 
                    break;
                case 'B': 
                case 'b': 
                case 'a': 
                    totalSize += 1; 
                    break;
                case 's': 
                case 'S':
                    totalSize += 4 + Encoding.UTF8.GetByteCount((string)value[i]);
                    break;
                case 'c':
                    totalSize += 2 + Encoding.UTF8.GetByteCount((string)value[i]);
                    break;
                case '#':
                    totalSize += (int)value[i];
                break;
            }
        }
        byte[] buffer = new byte[totalSize];
        Span<byte> span = buffer;

        int valueLength = 0;
        for (int i = 0; i < command.Length; i++)
        {
            try
            {
                switch (command[i])
                {
                    case 'Q':
                    case 'q':
                        BinaryPrimitives.WriteInt64BigEndian(span, Convert.ToInt64(value[i]));
                        span = span[8..];
                        break;

                    case 'B':
                    case 'b':
                        span[0] = Convert.ToByte(value[i]);
                        span = span[1..];
                        break;

                    case 'I':
                    case 'L':
                        BinaryPrimitives.WriteInt32BigEndian(span, Convert.ToInt32(value[i]));
                        span = span[4..];
                        break;

                    case 'H':
                        BinaryPrimitives.WriteUInt16BigEndian(span, Convert.ToUInt16(value[i]));
                        span = span[2..];
                        break;

                    case 'h':
                        BinaryPrimitives.WriteUInt16LittleEndian(span, Convert.ToUInt16(value[i]));
                        span = span[2..];
                        break;

                    case 's':
                    case 'S':
                        int strlength = Encoding.UTF8.GetByteCount((string)value[i]);
                        BinaryPrimitives.WriteInt32BigEndian(span, strlength);
                        span = span[4..];
                        Encoding.UTF8.GetBytes((string)value[i], span);
                        span = span[strlength..];
                        break;

                    case 'c':
                        strlength = Encoding.UTF8.GetByteCount((string)value[i]);
                        BinaryPrimitives.WriteUInt16BigEndian(span, Convert.ToUInt16(strlength));
                        span = span[2..];
                        Encoding.UTF8.GetBytes((string)value[i], span);
                        span = span[strlength..];
                        break;

                    case 'a':
                        span[0] = (bool)value[i] ? (byte)1 : (byte)0;
                        span = span[1..];
                        break;
                    case '#':
                        valueLength = (int)value[i];
                    break;
                    case 'v':
                        ((byte[])value[i]).CopyTo(span);
                        span = span[valueLength..];
                    break;

                    default:
                        Log.Warn("Unknown command character '{0}' at index {1}", command[i], i);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error writing command '{0}' at index {1}", command[i], i);
            }
        }
        return buffer;
    }
}
