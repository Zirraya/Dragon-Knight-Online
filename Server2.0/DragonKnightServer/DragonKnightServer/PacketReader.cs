using System.Text;

namespace DragonKnightServer.Protocol;

public class PacketReader
{
    private readonly byte[] data;
    private readonly int length;   // реальная длина полезных данных, а не размер буфера
    private int offset;

    public PacketReader(byte[] data) : this(data, data.Length) { }

    public PacketReader(byte[] data, int length)
    {
        this.data = data;
        this.length = length;
        this.offset = 0;
    }

    public int Offset => offset;
    public int Remaining => length - offset;

    public byte ReadByte()
    {
        if (offset + 1 > length) return 0;
        return data[offset++];
    }

    public void Skip(int count) => offset += count;

    // ВАЖНО: клиент шлёт 16-битные числа в BIG-ENDIAN (см. дамп: 00 45 = 69).
    // Раньше здесь читалось little-endian, из-за чего длина/opcode ломались.
    public ushort ReadUShort()
    {
        if (offset + 2 > length) return 0;
        ushort value = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;
        return value;
    }

    // little-endian вариант — на случай полей, которые действительно LE
    public ushort ReadUShortLE()
    {
        if (offset + 2 > length) return 0;
        ushort value = (ushort)(data[offset] | (data[offset + 1] << 8));
        offset += 2;
        return value;
    }

    public int ReadInt()
    {
        if (offset + 4 > length) return 0;
        int v = BitConverter.ToInt32(data, offset);
        offset += 4;
        return v;
    }

    // Строка до нулевого байта (как в реальном пакете: "harpie\0", "!<md5>\0", "<mac>\0")
    public string ReadString()
    {
        int start = offset;

        while (offset < length && data[offset] != 0)
            offset++;

        string s = offset > start
            ? Encoding.ASCII.GetString(data, start, offset - start)
            : "";

        if (offset < length) offset++; // пропускаем нулевой байт, если он есть
        return s;
    }
}
