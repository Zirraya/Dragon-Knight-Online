using System.Globalization;

namespace DragonKnightServer;

internal record AllianceNpc(int Id, ushort Template, int X, int Y, int Z, string Name);

internal static class AlliancePopulation
{
    private static readonly IReadOnlyList<AllianceNpc> Mainland = LoadMainland();

    internal static IReadOnlyList<AllianceNpc> ForMap(int map) => map == 1 ? Mainland : [];

    private static IReadOnlyList<AllianceNpc> LoadMainland()
    {
        using var stream = typeof(AlliancePopulation).Assembly.GetManifestResourceStream("AllianceNpcs.tsv")
            ?? throw new InvalidOperationException("В сборке отсутствуют данные НПС Альянса.");
        using var reader = new StreamReader(stream);
        var npcs = new List<AllianceNpc>();
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            var fields = line.Split('\t', 6);
            npcs.Add(new AllianceNpc(1_100_000 + int.Parse(fields[0], CultureInfo.InvariantCulture),
                ushort.Parse(fields[1], CultureInfo.InvariantCulture),
                int.Parse(fields[2], CultureInfo.InvariantCulture), int.Parse(fields[3], CultureInfo.InvariantCulture),
                int.Parse(fields[4], CultureInfo.InvariantCulture), fields[5]));
        }
        return npcs;
    }
}
