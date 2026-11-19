using DragonKnightServer.Database;

namespace DragonKnightServer;

internal static class StarterEquipment
{
    private static readonly CharacterItem[] Paladin =
        [new(1, 485), new(2, 486), new(3, 488), new(4, 487), new(6, 398)];
    private static readonly CharacterItem[] Warrior =
        [new(1, 490), new(2, 491), new(3, 493), new(4, 492), new(6, 398)];
    private static readonly CharacterItem[] Wanderer =
        [new(1, 495), new(2, 496), new(3, 498), new(4, 497), new(6, 396), new(7, 395)];
    private static readonly CharacterItem[] Mage =
        [new(1, 24001), new(2, 25001), new(3, 27001), new(4, 26001), new(6, 397)];
    private static readonly CharacterItem[] Priest =
        [new(1, 24002), new(2, 25004), new(3, 27004), new(4, 26004), new(6, 397)];

    public static IReadOnlyList<CharacterItem> ForTemplate(int template) => template switch
    {
        1 or 6 => Paladin,
        2 or 7 => Warrior,
        3 or 8 => Wanderer,
        4 or 9 => Mage,
        5 or 10 => Priest,
        _ => []
    };
}
