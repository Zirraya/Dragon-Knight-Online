using System.Text;

namespace DragonKnightServer.Protocol;

public class PacketReader
{
    private readonly byte[] data;
    private int offset;

    public PacketReader(byte[] data)
    {
        this.data = data;
        this.offset = 0;
    }

    public ushort ReadUShort()
    {
        if (offset + 1 >= data.Length)
            return 0;

        ushort value = (ushort)(data[offset] | (data[offset + 1] << 8));
        offset += 2;
        return value;
    }

    public int ReadInt()
    {
        if (offset + 3 >= data.Length)
            return 0;

        int v = BitConverter.ToInt32(data, offset);
        offset += 4;
        return v;
    }

    public string ReadString()
    {
        int start = offset;

        while (offset < data.Length && data[offset] != 0)
            offset++;

        if (offset > start)
        {
            string s = Encoding.UTF8.GetString(data, start, offset - start);
            offset++; // Пропускаем нулевой байт
            return s;
        }

        offset++;
        return "";
    }
}