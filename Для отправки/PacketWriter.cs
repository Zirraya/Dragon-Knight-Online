using System.Text;

namespace DragonKnightServer;

public class PacketWriter
{
    private MemoryStream stream = new MemoryStream();
    private BinaryWriter writer;

    public PacketWriter()
    {
        writer = new BinaryWriter(stream);
    }

    public void Write(ushort value)
    {
        writer.Write(value);
    }

    public void Write(int value)
    {
        writer.Write(value);
    }

    public void Write(short value)
    {
        writer.Write(value);
    }

    public void Write(byte value)
    {
        writer.Write(value);
    }

    public void Write(byte[] value)
    {
        writer.Write(value);
    }

    public void WriteString(string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        writer.Write(data);
        writer.Write((byte)0);
    }

    public byte[] ToArray()
    {
        return stream.ToArray();
    }

    public void Reset()
    {
        stream.SetLength(0);
        stream.Position = 0;
    }
}