using System.Text;

public class HexDump
{
    public static string Dump(ReadOnlySpan<byte> buffer, int bytesPerLine = 16)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesPerLine);

        var sb = new StringBuilder();

        for (int i = 0; i < buffer.Length; i += bytesPerLine)
        {
            var line = buffer.Slice(i, Math.Min(bytesPerLine, buffer.Length - i));

            sb.Append(i.ToString("X8"));
            sb.Append(": ");

            // Hex-байты
            for (int j = 0; j < bytesPerLine; j++)
            {
                if (j < line.Length)
                    sb.Append(line[j].ToString("X2"));
                else
                    sb.Append("  ");

                sb.Append(' ');
            }

            sb.Append('|');
            for (int j = 0; j < line.Length; j++)
            {
                byte b = line[j];
                sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
            }
            sb.AppendLine("|");
        }

        return sb.ToString();
    }
}
