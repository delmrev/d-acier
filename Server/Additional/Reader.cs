using NLog;
using System.Text;

public class Reader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static object[] ReadBytes(byte[] packet, string command)
    {
        using MemoryStream m = new(packet);
        using BinaryReader reader = new(m);
        return ReadBytes(reader, command);
    }

    public static object[] ReadBytes(BinaryReader reader, string command)
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
                        objects[i] = reader.ReadByte();
                        break;

                    case 'I':
                    case 'L':
                        objects[i] = ReadInt32Be(reader);
                        break;

                    case 'Q':
                    case 'q':
                        objects[i] = ReadInt64Be(reader);
                        break;

                    case 'H':
                        objects[i] = ReadInt16Be(reader);
                        break;
                    case 'h':
                    objects[i] = ReadInt16Le(reader);
                        break;
                    case 's':
                    case 'S':
                        objects[i] = UnpackString(reader);
                        break;

                    case 'c':
                        short length = ReadInt16Be(reader);
                        byte[] stringBytes = reader.ReadBytes(length);
                        objects[i] = Encoding.UTF8.GetString(stringBytes);
                        break;

                    default:
                        Log.Warn("Unknown command character '{0}' at index {1}", command[i], i);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error reading command '{0}' at index {1}", command[i], i);
            }
        }

        return objects;
    }
    public static async Task<(bool success, object[] output)> TryReadBytes(byte[] packet, string command)
    {
        using MemoryStream m = new(packet);
        using BinaryReader reader = new(m);
        return await TryReadBytes(command,reader);
    }
    public static async Task<(bool success, object[] output)> TryReadBytes(string command, BinaryReader reader)
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
                        objects[i] = reader.ReadByte();
                        break;

                    case 'I':
                    case 'L':
                        objects[i] = ReadInt32Be(reader);
                        break;

                    case 'Q':
                    case 'q':
                        objects[i] = ReadInt64Be(reader);
                        break;

                    case 'H':
                        objects[i] = ReadInt16Be(reader);
                        break;
                    case 'h':
                    objects[i] = ReadInt16Le(reader);
                        break;
                    case 's':
                    case 'S':
                        objects[i] = UnpackString(reader);
                        break;

                    case 'c':
                        short length = ReadInt16Be(reader);
                        byte[] stringBytes = reader.ReadBytes(length);
                        objects[i] = Encoding.UTF8.GetString(stringBytes);
                        break;

                    default:
                        Log.Warn("Unknown command character '{0}' at index {1}", command[i], i);
                        break;
                }
            }
            catch
            {
                return (false,objects);
            }
        }

        
        return (true,objects);
    }
    public static int ReadInt32Be(BinaryReader reader)
    {
        byte[] buffer = reader.ReadBytes(4);
        if (buffer.Length < 4)
        {
            Log.Debug("Not enough bytes to read Int32");
            return -90;
        }
        if(BitConverter.IsLittleEndian){
            Array.Reverse(buffer);
        }
        return BitConverter.ToInt32(buffer, 0);
    }

    public static long ReadInt64Be(BinaryReader reader)
    {
        byte[] buffer = reader.ReadBytes(8);
        if (buffer.Length < 8)
        {
            Log.Debug("Not enough bytes to read Int64");
            return 0;
        }
        if(BitConverter.IsLittleEndian){
            Array.Reverse(buffer);
        }
        return BitConverter.ToInt64(buffer, 0);
    }

    public static short ReadInt16Be(BinaryReader reader)
    {
        byte[] buffer = reader.ReadBytes(2);
        if (buffer.Length < 2)
        {
            Log.Debug("Not enough bytes to read Int16");
            return 0;
        }
        return (short)((buffer[0] << 8) | buffer[1]);
    }
    public static short ReadInt16Le(BinaryReader reader)
    {
        byte[] buffer = reader.ReadBytes(2);
        if (buffer.Length < 2)
        {
            Log.Debug("Not enough bytes to read Int16");
            return 0;
        }
        return BitConverter.ToInt16(buffer);
    }

    public static string UnpackString(BinaryReader reader)
    {
        int length = ReadInt32Be(reader);
        if (length <= 0)
        {
            Log.Warn("UnpackString read zero or negative length: {0}", length);
            return string.Empty;
        }

        byte[] stringBytes = reader.ReadBytes(length);
        if (stringBytes.Length != length)
        {
            Log.Debug("UnpackString expected {0} bytes, got {1}", length, stringBytes.Length);
        }

        string resultString = Encoding.UTF8.GetString(stringBytes);
        return resultString;
    }
    public static long Readint64Le(BinaryReader reader)
    {
        byte[] buffer = reader.ReadBytes(8);
        if (buffer.Length < 8)
        {
            Log.Debug("Not enough bytes to read Int64");
            return 0;
        }
        if(!BitConverter.IsLittleEndian){
            Array.Reverse(buffer);
        }
        return BitConverter.ToInt64(buffer, 0);
    }
    public static byte[] ReadBuf(int size, BinaryReader reader) // #v
    {
        var buf = reader.ReadBytes(size);
        return buf;
    }
}
