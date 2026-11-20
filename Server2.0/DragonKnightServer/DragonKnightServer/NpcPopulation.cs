using System.Globalization;

namespace DragonKnightServer;

internal record WorldNpc(int Id, ushort Template, int X, int Y, int Z, string Name, bool IsDragonEnvoy,
    int InfoId);

internal static class NpcPopulation
{
    // NPC actor IDs in the client are 1,000,000 + the map actor ID. Most
    // records use the npc_info ID as that actor ID. Dragon Envoys are an
    // exception and keep a separate npc_info ID for shop requests.
    internal const int IdOffset = 1_000_000;
    private static readonly IReadOnlyList<WorldNpc> Mainland = LoadMainland();

    internal static IReadOnlyList<WorldNpc> ForMap(int map) => map == 1 ? Mainland : [];

    private static IReadOnlyList<WorldNpc> LoadMainland()
    {
        var all = Load("AllianceNpcs.tsv")
            .Concat(Load("FederationNpcs.tsv"))
            .Concat(Load("DragonEnvoys.tsv", isDragonEnvoy: true));
        return [.. all.GroupBy(npc => npc.Id)
            .Select(group => group.FirstOrDefault(npc => npc.IsDragonEnvoy) ?? group.First())];
    }

    private static IEnumerable<WorldNpc> Load(string resource, bool isDragonEnvoy = false)
    {
        using var stream = typeof(NpcPopulation).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"В сборке отсутствуют данные НПС {resource}.");
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            // DragonEnvoys.tsv has an optional seventh actor-ID column.
            var fields = line.Split('\t', 7);
            int infoId = int.Parse(fields[0], CultureInfo.InvariantCulture);
            int actorId = fields.Length == 7
                ? int.Parse(fields[6], CultureInfo.InvariantCulture)
                : infoId;
            yield return new WorldNpc(IdOffset + actorId,
                ushort.Parse(fields[1], CultureInfo.InvariantCulture),
                int.Parse(fields[2], CultureInfo.InvariantCulture), int.Parse(fields[3], CultureInfo.InvariantCulture),
                int.Parse(fields[4], CultureInfo.InvariantCulture), fields[5], isDragonEnvoy, infoId);
        }
    }
}
