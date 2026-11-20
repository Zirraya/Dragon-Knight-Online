using System.Buffers.Binary;

namespace DragonKnightServer;

internal static class CharacterOrigins
{
    internal const ushort Alliance = 1;
    internal const ushort Federation = 2;

    internal static readonly (int X, int Y, int Z) AllianceStart = (452383, 159581, 1557);

    internal static ushort ReadFaction(ReadOnlySpan<byte> appearance) =>
        appearance.Length >= sizeof(ushort) && BinaryPrimitives.ReadUInt16BigEndian(appearance) == Federation
            ? Federation : Alliance;

    internal static (int X, int Y, int Z) ForFaction(ushort faction) => faction == Federation
        // Алая земля, площадка рядом с ветераном Гаром из npc_info.bin, точка 2456.
        ? (234793, 96959, 2486)
        : AllianceStart;
}
