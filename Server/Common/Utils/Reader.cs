using NLog;
using System.Buffers.Binary;
using System.Text;

public class Reader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static object[] ReadBytes(ref ReadOnlySpan<byte> span, string command)
    {
        var (_, output) = TryReadBytes(ref span, command);
        return output;
    }
    public static object[] ReadBytes(byte[] packet, string command)
    {
        ReadOnlySpan<byte> span = packet.AsSpan();
        var (_, output) = TryReadBytes(ref span, command);
        return output;
    }
    public static (bool success, object[] output) TryReadBytes(byte[] packet, string command)
    {
        ReadOnlySpan<byte> span = packet.AsSpan();
        return TryReadBytes(ref span, command);
    }
    public static (bool success, object[] output) TryReadBytes(ref ReadOnlySpan<byte> span, string command)
    {
        object[] objects = new object[command.Length];

        for (int i = 0; i < command.Length; i++)
        {
            try
            {
                switch (command[i])
                {
                    case 'B':
                    case 'b':
                        objects[i] = span[0];
                        span = span[1..];
                        break;

                    case 'I':
                    case 'L':
                        objects[i] = BinaryPrimitives.ReadInt32BigEndian(span);
                        span = span[4..];
                        break;
                    case 'l':
                        objects[i] = BinaryPrimitives.ReadInt64LittleEndian(span);
                        span = span[8..];
                    break;

                    case 'Q':
                    case 'q':
                        objects[i] = BinaryPrimitives.ReadInt64BigEndian(span);
                        span = span[8..];
                        break;

                    case 'H':
                        objects[i] = BinaryPrimitives.ReadUInt16BigEndian(span);
                        span = span[2..];
                        break;

                    case 'h':
                        objects[i] = BinaryPrimitives.ReadInt16LittleEndian(span);
                        span = span[2..];
                        break;

                    case 's':
                    case 'S':
                        int length = BinaryPrimitives.ReadInt32BigEndian(span);
                        span = span[4..];

                        if (length <= 0)
                        {
                            objects[i] = string.Empty;
                            break;
                        }

                        objects[i] = Encoding.UTF8.GetString(span[..length]);
                        span = span[length..];
                        break;

                    case 'c':
                        ushort ulength = BinaryPrimitives.ReadUInt16BigEndian(span);
                        span = span[2..];

                        objects[i] = Encoding.UTF8.GetString(span[..ulength]);
                        span = span[ulength..];
                        break;

                    case 'a':
                        objects[i] = span[0] != 0;
                        span = span[1..];
                        break;

                    default:
                        Log.Warn("Unknown command character '{0}' at index {1}", command[i], i);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error reading command '{0}' at index {1}", command[i], i);
                return (false, objects);
            }
        }

        return (true, objects);
    }
}
