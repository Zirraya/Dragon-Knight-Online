using DragonKnightServer.Database;

namespace DragonKnightServer;

internal static class StarterEquipment
{
    private static readonly CharacterItem[] Paladin =
        [new(1, 485, EquipmentSlot: 1), new(2, 486, EquipmentSlot: 2),
         new(3, 488, EquipmentSlot: 3), new(4, 487, EquipmentSlot: 4),
         new(6, 398, EquipmentSlot: 6)];
    private static readonly CharacterItem[] Warrior =
        [new(1, 490, EquipmentSlot: 1), new(2, 491, EquipmentSlot: 2),
         new(3, 493, EquipmentSlot: 3), new(4, 492, EquipmentSlot: 4),
         new(6, 398, EquipmentSlot: 6)];

    // Стартовый странник из захвата создаётся с шаблоном 3. В 0x0D97
    // сервер отправляет четыре предмета без головного убора.
    private static readonly CharacterItem[] Wanderer =
        [new(1, 24003, EquipmentSlot: 1), new(2, 25005, EquipmentSlot: 2),
         new(3, 27005, EquipmentSlot: 3), new(4, 26005, EquipmentSlot: 4)];
    private static readonly CharacterItem[] FemaleWanderer =
        [new(1, 474, EquipmentSlot: 1), new(2, 475, EquipmentSlot: 2),
         new(3, 477, EquipmentSlot: 3), new(4, 476, EquipmentSlot: 4),
         new(6, 478, EquipmentSlot: 6)];
    private static readonly CharacterItem[] Mage =
        [new(1, 24001, EquipmentSlot: 1), new(2, 25001, EquipmentSlot: 2),
         new(3, 27001, EquipmentSlot: 3), new(4, 26001, EquipmentSlot: 4),
         new(6, 397, EquipmentSlot: 6)];
    private static readonly CharacterItem[] Priest =
        [new(1, 24002, EquipmentSlot: 1), new(2, 25004, EquipmentSlot: 2),
         new(3, 27004, EquipmentSlot: 3), new(4, 26004, EquipmentSlot: 4),
         new(6, 397, EquipmentSlot: 6)];

    public static IReadOnlyList<CharacterItem> ForTemplate(int template) => template switch
    {
        1 or 6 => Paladin,
        2 or 7 => Warrior,
        3 => Wanderer,
        8 => FemaleWanderer,
        4 or 9 => Mage,
        5 or 10 => Priest,
        _ => []
    };
}
