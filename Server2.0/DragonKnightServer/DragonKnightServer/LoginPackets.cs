using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using DragonKnightServer.Database;

namespace DragonKnightServer.Protocol;

internal static class LoginPackets
{
    public const int OpcodeOffset = 6;
    public const int HeaderSize = 8;
    public const int CharacterSlots = 9;
    public const int AppearanceSize = 76;

    internal static readonly Encoding NameEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1251)!;

    public static void WriteHeader(Span<byte> packet, Opcode opcode)
    {
        BinaryPrimitives.WriteUInt16BigEndian(packet, checked((ushort)packet.Length));
        BinaryPrimitives.WriteUInt16BigEndian(packet[OpcodeOffset..], (ushort)opcode);
    }

    public static byte[] CreateResult(Opcode opcode, ushort result)
    {
        var packet = new byte[HeaderSize + sizeof(ushort)];
        WriteHeader(packet, opcode);
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(HeaderSize), result);
        return packet;
    }

    public static byte[] CreateCharacterList(IReadOnlyList<LoginCharacter> characters, Opcode opcode = Opcode.CharacterList)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Фиксированный префикс перед первой записью, включая float 1.0 в big-endian.
        writer.Write(new byte[14]);
        writer.Write(BinaryPrimitives.ReverseEndianness(0x3F800000));
        writer.Write(new byte[8]);

        foreach (var character in characters)
        {
            writer.Write((byte)1);
            WriteString(writer, character.Name);
            writer.Write(BinaryPrimitives.ReverseEndianness((ushort)Math.Clamp(character.Level, 0, ushort.MaxValue)));
            writer.Write(BinaryPrimitives.ReverseEndianness(character.Template));

            if (character.Appearance is { Length: AppearanceSize })
            {
                writer.Write(character.Appearance);
            }
            else
            {
                // Для персонажей, созданных до сохранения блока внешности.
                writer.Write(BinaryPrimitives.ReverseEndianness((ushort)1));
                writer.Write(BinaryPrimitives.ReverseEndianness((ushort)0x40));
                writer.Write(new byte[AppearanceSize - 4]);
            }

            WriteString(writer, "map" + character.Map.ToString("D3", CultureInfo.InvariantCulture));
            writer.Write(new byte[12]);
        }

        // В дампе после двух записей семь нулевых байтов и завершающий флаг 1.
        writer.Write(new byte[CharacterSlots - characters.Count]);
        writer.Write((byte)1);

        byte[] packet = stream.ToArray();
        WriteHeader(packet, opcode);
        return packet;
    }

    internal static string? ReadCharacterName(ReadOnlySpan<byte> packet)
    {
        var body = packet[HeaderSize..];
        if (!TryReadString(ref body, out var name) || !body.IsEmpty || name.Length is < 1 or > 32)
            return null;

        return NameEncoding.GetString(name);
    }

    internal static bool TryReadString(ref ReadOnlySpan<byte> body, out ReadOnlySpan<byte> value)
    {
        value = default;
        if (body.Length < sizeof(ushort))
            return false;

        int length = BinaryPrimitives.ReadUInt16BigEndian(body);
        var bytes = body[sizeof(ushort)..];
        if (length < 1 || bytes.Length < length || bytes[length - 1] != 0)
            return false;

        value = bytes[..(length - 1)];
        body = bytes[length..];
        return value.IndexOf((byte)0) < 0;
    }

    internal static void WriteString(BinaryWriter writer, string value)
    {
        byte[] encoded = NameEncoding.GetBytes(value);
        writer.Write(BinaryPrimitives.ReverseEndianness(checked((ushort)(encoded.Length + 1))));
        writer.Write(encoded);
        writer.Write((byte)0);
    }
}
