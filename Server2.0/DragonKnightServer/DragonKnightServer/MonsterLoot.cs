using System.Globalization;

namespace DragonKnightServer;

internal readonly record struct LootDrop(ushort ItemId, ushort StackLimit);

internal static class MonsterLoot
{
    private const int DropMultiplier = 2;
    private const int MaterialChance = 25;
    private const int PotionChance = 10;
    private static readonly IReadOnlyDictionary<ushort, LootDrop> Materials = LoadMaterials();

    internal static LootDrop? Roll(ushort template, int roll)
    {
        if (!Materials.TryGetValue(template, out var material))
            return null;
        if (roll < MaterialChance * DropMultiplier)
            return material;
        if (roll < (MaterialChance + PotionChance) * DropMultiplier)
            // Зелья восстановления здоровья и магии из item.bin имеют одинаковый лимит стопки.
            return new LootDrop(roll % 2 == 0 ? (ushort)10002 : (ushort)10005, 100);
        return null;
    }

    private static Dictionary<ushort, LootDrop> LoadMaterials()
    {
        using var stream = typeof(MonsterLoot).Assembly.GetManifestResourceStream("MonsterLoot.tsv")
            ?? throw new InvalidOperationException("В сборке отсутствует таблица добычи.");
        using var reader = new StreamReader(stream);
        var materials = new Dictionary<ushort, LootDrop>();
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            var fields = line.Split('\t', 3);
            var drop = new LootDrop(ushort.Parse(fields[0], CultureInfo.InvariantCulture),
                ushort.Parse(fields[1], CultureInfo.InvariantCulture));
            foreach (string template in fields[2].Split(','))
                materials.Add(ushort.Parse(template, CultureInfo.InvariantCulture), drop);
        }
        return materials;
    }
}
