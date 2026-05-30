using System;
using System.Collections.Generic;
using System.Text;
using NLog;

public class Writer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static List<byte> WriteBytes(string command, params object[] value)
    {
        List<byte> buffer = new();
        byte[] localBuffer;
        byte[] length;

        if (command.Length != value.Length)
        {
            Log.Warn("Command length {0} does not match value length {1}", command.Length, value.Length);
        }

        for (int i = 0; i < command.Length; i++)
        {
            try
            {
                switch (command[i])
                {
                    case 'Q':
                    case 'q':
                        localBuffer = BitConverter.GetBytes(Convert.ToInt64(value[i]));
                        Array.Reverse(localBuffer);
                        buffer.AddRange(localBuffer);
                        break;

                    case 'B':
                    case 'b':
                        buffer.Add(Convert.ToByte(value[i]));
                        break;

                    case 'I':
                    case 'L':
                        localBuffer = BitConverter.GetBytes(Convert.ToInt32(value[i]));
                        Array.Reverse(localBuffer);
                        buffer.AddRange(localBuffer);
                        break;

                    case 'H':
                        localBuffer = BitConverter.GetBytes(Convert.ToInt16(value[i]));
                        Array.Reverse(localBuffer);
                        buffer.AddRange(localBuffer);
                        break;
                    case 'h':
                        localBuffer = BitConverter.GetBytes(Convert.ToUInt16(value[i]));
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(localBuffer);
                        }
                        buffer.AddRange(localBuffer);
                        break;
                    case 's':
                    case 'S':
                        localBuffer = Encoding.UTF8.GetBytes((string)value[i]);
                        length = BitConverter.GetBytes(localBuffer.Length);
                        Array.Reverse(length);
                        buffer.AddRange(length);
                        buffer.AddRange(localBuffer);
                        break;

                    case 'c':
                        localBuffer = Encoding.UTF8.GetBytes((string)value[i]);
                        length = BitConverter.GetBytes((short)localBuffer.Length);
                        Array.Reverse(length);
                        buffer.AddRange(length);
                        buffer.AddRange(localBuffer);
                        break;

                    case 'a':
                        buffer.Add((bool)value[i] ? (byte)1 : (byte)0);
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

        //Log.Trace("Serialized {0} bytes", buffer.Count);
        return buffer;
    }
}
