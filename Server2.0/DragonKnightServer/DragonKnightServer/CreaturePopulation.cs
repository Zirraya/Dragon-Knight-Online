using System.Globalization;

namespace DragonKnightServer;

internal sealed record CreatureTemplate(ushort Id, int Level, int HitPoints, int Mana, int CorpseMs, int RefreshMs,
    string? AirborneName = null);

internal sealed record CreatureSpawn(int Id, CreatureTemplate Template, (int X, int Y, int Z) Position);

internal static class CreaturePopulation
{
    private const int CellSize = 16_384;
    private static readonly ILookup<(int X, int Y), CreatureSpawn> Mainland = Load("WorldCreatures.tsv", 1_200_000)
        .Concat(Load("AirCreatures.tsv", 1_300_000, airborne: true))
        .ToLookup(spawn => (spawn.Position.X / CellSize, spawn.Position.Y / CellSize));

    internal static IEnumerable<CreatureSpawn> Near(int map, int x, int y, int distance)
    {
        if (map != 1)
            yield break;

        int firstX = (int)(((long)x - distance) / CellSize);
        int lastX = (int)(((long)x + distance) / CellSize);
        int firstY = (int)(((long)y - distance) / CellSize);
        int lastY = (int)(((long)y + distance) / CellSize);
        for (int cellY = firstY; cellY <= lastY; cellY++)
        {
            for (int cellX = firstX; cellX <= lastX; cellX++)
            {
                foreach (var spawn in Mainland[(cellX, cellY)])
                    yield return spawn;
            }
        }
    }

    private static IEnumerable<CreatureSpawn> Load(string resource, int nextId, bool airborne = false)
    {
        using var stream = typeof(CreaturePopulation).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"В сборке отсутствуют данные существ {resource}.");
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            var fields = line.Split('\t', airborne ? 8 : 7);
            var template = new CreatureTemplate(ushort.Parse(fields[0], CultureInfo.InvariantCulture),
                Parse(fields[1]), Parse(fields[2]), Parse(fields[3]), Parse(fields[4]), Parse(fields[5]),
                airborne ? fields[6] : null);
            foreach (string point in fields[^1].Split(';'))
            {
                var coordinates = point.Split(',');
                yield return new CreatureSpawn(nextId++, template,
                    (Parse(coordinates[0]), Parse(coordinates[1]), Parse(coordinates[2])));
            }
        }
    }

    private static int Parse(string value) => int.Parse(value, CultureInfo.InvariantCulture);
}
