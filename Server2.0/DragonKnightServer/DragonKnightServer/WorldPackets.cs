using System.Buffers.Binary;
using System.Globalization;
using DragonKnightServer.Database;

namespace DragonKnightServer.Protocol;

internal static partial class WorldPackets
{
    public const int MoveRequestSize = 31;
    public const byte FirstBackpackPage = 2;
    public const int BackpackPageSize = 36;
    public const int BackpackPageCount = 5;
    public const int QuickbarMaxSize = 4096;
    public const byte GrowingDragonType = 2;
    public const byte BattleDragonType = 4;
    public const ushort FirstBattleDragonSlotTag = 50;
    public const byte ActiveBattleDragonSlot = 5;
    public const ushort BattleDragonSummonSkill = 28001;
    public const int BattleDragonSummonTimeMs = 1500;
    private const int WalkingSpeed = 700;
    private const int ElvenDragonFlightSpeed = 1800;
    public static IReadOnlyList<ushort> DragonSkills { get; } = [6961];
    private static readonly IReadOnlyDictionary<ushort, (ushort Template, string Name)> YoungDragonAppearances =
        new Dictionary<ushort, (ushort, string)>
        {
            [CharacterRepository.ElvenDragonItem] = (6018, "Эльфийский дракон"),
            [58503] = (7503, "Молодой Белый дракон"),
            [58504] = (7504, "Молодой Черный дракон"),
            [58505] = (7505, "Молодой Синий дракон"),
            [58506] = (7506, "Молодой Золотой дракон"),
            [58507] = (7507, "Молодой Красный дракон"),
            [58508] = (7508, "Молодой Серебряный дракон"),
            [58509] = (7509, "Молодой Бронзовый дракон"),
            [41018] = (6011, "Молодой Красный дракон"),
            [41019] = (6012, "Молодой Белый дракон"),
            [41020] = (6013, "Молодой Синий дракон"),
            [41021] = (6014, "Молодой Черный дракон"),
            [41022] = (6015, "Молодой Бронзовый дракон"),
            [41023] = (6016, "Молодой Серебряный дракон"),
            [41024] = (6017, "Молодой Золотой дракон")
        };
    private static readonly (ushort Id, int Value)[] BattleDragonAttributes =
    [
        (0, 85), (0x0D, 3000), (0x0F, 3000), (0x0E, 1500), (0x10, 1500), (0x0C, 100),
        (0x11, 30), (0x12, 30), (0x13, 150), (0x14, 30), (0x15, 150), (0x16, 20),
        (0x17, 100), (0x18, 150), (0x19, 20), (0x1A, 150), (0x1B, 100), (0x1C, 20),
        (0x1D, 150), (0x1E, 150), (0x1F, 100), (0x20, 15), (0x21, 15), (0x22, 15),
        (0x23, 15), (0x24, ElvenDragonFlightSpeed), (0x25, 0), (0x26, 0), (0x27, 0), (0x28, 0),
        (0x29, 0), (0x2A, 0), (0x2B, 0), (0x2C, 0), (0x2D, 0)
    ];
    private static readonly (ushort Id, int Value)[] YoungDragonAttributes =
    [
        (0x00, 10), (0x0C, 100), (0x0D, 3000), (0x0E, 100), (0x0F, 3000), (0x10, 100),
        (0x11, 570), (0x12, 180), (0x13, 80), (0x14, 90),
        (0x15, 0), (0x16, 0), (0x17, 0), (0x18, 0), (0x19, 85), (0x1A, 0),
        (0x1B, 6), (0x1C, 6), (0x1D, 6), (0x1E, 6), (0x1F, 6),
        (0x20, 2500), (0x21, 80), (0x22, 100)
    ];

    public static byte[][] CreateEntry(WorldCharacter character)
    {
        var position = StartingPosition(character);
        int tick = Environment.TickCount;
        var equipment = EquippedItems(character);
        return
        [
            Build(Opcode.WorldEntry, writer =>
            {
                WriteUInt16(writer, 0);
                LoginPackets.WriteString(writer, "map" + character.Map.ToString("D3", CultureInfo.InvariantCulture));
                WritePosition(writer, position);
                WriteInt32(writer, checked((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            }),
            Build(Opcode.PlayerSpawn, writer => WriteSpawn(writer, character, position, equipment, tick)),
            Build(Opcode.PlayerStatus, writer => WriteStatus(writer, character, tick)),
            CreatePlayerAttributes(character),
            Build(Opcode.InitialPlayerState, writer => WriteInitialState(writer, character)),
            .. CreateBackpackLayout(tick),
            CreateInventory(character.Id, character.Silver, character.Inventory, unchecked(tick + 1)),
            CreateDragonSlots(character.Id, character.Inventory),
            CreateEquipmentDetails(character.Id, equipment, tick),
            CreateEquipmentList(character, equipment, tick),
            CreateEquipmentSlotFlags(),
            CreateEquipmentItemList(character.Id, equipment),
            CreateLearnedSkills(0, WorldSession.GetPlayerSkills(character.Template)),
            Build(Opcode.WorldEntryStatus, writer => WriteUInt16(writer, 0)),
            Build(Opcode.WorldEntryComplete)
        ];
    }

    internal static IReadOnlyList<CharacterItem> EquippedItems(WorldCharacter character)
    {
        var defined = character.Inventory.Where(item => item.EquipmentSlot.HasValue).ToArray();
        return defined.Length > 0
            ? defined.Where(item => item.IsEquipped).ToArray()
            : StarterEquipment.ForTemplate(character.Template);
    }

    public static byte[] CreateClock() => Build(Opcode.WorldClock, writer =>
    {
        WriteInt32(writer, 0);
        WriteInt32(writer, Environment.TickCount);
    });

    internal static byte[] CreatePlayerStatus(WorldCharacter character) =>
        Build(Opcode.PlayerStatus, writer => WriteStatus(writer, character, Environment.TickCount));

    internal static byte[] CreatePlayerAttributes(WorldCharacter character) => Build(Opcode.PlayerAttributes, writer =>
    {
        WriteInt32(writer, character.Id);
        WriteUInt16(writer, 5);
        WriteVitalAttributes(writer, character);
        WriteAttribute(writer, 0x24, WalkingSpeed);
        // В 0x0D92 отметка времени записана в little-endian.
        writer.Write(Environment.TickCount);
    });

    internal static byte[] CreatePlayerProgress(WorldCharacter character) => Build(Opcode.AttributeChange, writer =>
    {
        var stats = CharacterProgression.For(character.Template, character.Level);
        WriteInt32(writer, character.Id);
        WriteUInt16(writer, 10);
        WriteAttribute(writer, 0, character.Level);
        WriteAttribute(writer, 0x0D, character.HitPoints);
        WriteAttribute(writer, 0x0E, character.Mana);
        WriteAttribute(writer, 0x0F, stats.HitPoints);
        WriteAttribute(writer, 0x10, stats.Mana);
        WriteAttribute(writer, 0x11, stats.Strength);
        WriteAttribute(writer, 0x12, stats.Constitution);
        WriteAttribute(writer, 0x13, stats.Intellect);
        WriteAttribute(writer, 0x14, stats.Potential);
        WriteAttribute(writer, 0x18,
            CharacterProgression.ExperienceWithinLevel(character.Experience, character.Level));
        writer.Write(Environment.TickCount);
    });

    public static byte[] CreatePositionUpdate(int characterId, (int X, int Y, int Z) position, ushort movementMode,
        bool standing = false, bool mounted = false)
    {
        int tick = Environment.TickCount;
        int speed = mounted ? ElvenDragonFlightSpeed : WalkingSpeed;
        return Build(Opcode.PlayerPosition, writer =>
        {
            WriteInt32(writer, characterId);
            WritePosition(writer, position);
            WriteInt32(writer, standing ? 0 : speed);
            WriteUInt16(writer, standing ? (ushort)0 : (ushort)1);
            WriteUInt16(writer, movementMode);
            WriteInt32(writer, tick);
            writer.Write(tick);
        });
    }

    public static (int X, int Y, int Z) StartingPosition(WorldCharacter character)
    {
        var savedPosition = ((int)character.X, (int)character.Y, (int)character.Z);
        if (character.Map != 1)
            return savedPosition;

        ushort faction = CharacterOrigins.ReadFaction(character.Appearance);
        // Старые записи могут содержать координаты из схемы или прежнюю стартовую точку Альянса.
        return (character.X, character.Y, character.Z) == (100f, 200f, 0f)
            || (faction == CharacterOrigins.Federation && savedPosition == CharacterOrigins.AllianceStart)
            ? CharacterOrigins.ForFaction(faction) : savedPosition;
    }

    private static void WriteSpawn(BinaryWriter writer, WorldCharacter character,
        (int X, int Y, int Z) position, IReadOnlyList<CharacterItem> equipment, int tick)
    {
        WriteInt32(writer, character.Id);
        WriteInt32(writer, 0x224);
        WritePosition(writer, position);
        WriteUInt16(writer, 90);
        writer.Write(new byte[40]);
        WriteUInt16(writer, (ushort)Math.Clamp(character.Template, 1, ushort.MaxValue));
        WriteUInt16(writer, CharacterOrigins.ReadFaction(character.Appearance));

        WriteUInt16(writer, 5);
        WriteAttribute(writer, 0, character.Level);
        WriteVitalAttributes(writer, character);
        WriteUInt16(writer, 0);
        writer.Write((byte)equipment.Count);
        foreach (var item in equipment.Where(item => item.EquipmentSlot is byte))
        {
            WriteUInt16(writer, item.EquipmentSlot!.Value);
            WriteUInt16(writer, item.ItemId);
        }

        if (character.Appearance is { Length: LoginPackets.AppearanceSize } appearance)
            writer.Write(appearance.AsSpan(^8));
        else
            writer.Write(new byte[8]);

        LoginPackets.WriteString(writer, character.Name);
        writer.Write(new byte[] { 0, 1, 0, 0, 1 });
        writer.Write(new byte[53]);
        WriteInt32(writer, tick);
    }

    private static void WriteStatus(BinaryWriter writer, WorldCharacter character, int tick)
    {
        WriteUInt16(writer, 0);
        LoginPackets.WriteString(writer, character.Name);
        WriteInt32(writer, character.Id);

        var stats = CharacterProgression.For(character.Template, character.Level);
        (ushort Id, int Value)[] attributes =
        [
            (0, character.Level), (0x11, stats.Strength), (0x12, stats.Constitution),
            (0x13, stats.Intellect), (0x14, stats.Potential), (0x15, 100), (0x16, 1),
            (0x17, 0),
            (0x18, CharacterProgression.ExperienceWithinLevel(character.Experience, character.Level)),
            (0x19, 0), (0x1A, 0), (0x1B, 0), (0x1C, 0),
            (0x2B, 0), (0x2C, 0), (0x2D, 0), (0x2E, 0), (0x2F, 0)
        ];
        WriteUInt16(writer, (ushort)attributes.Length);
        foreach (var (id, value) in attributes)
            WriteAttribute(writer, id, value);

        // В 0x0C01 после пустых дополнительных разделов идёт блок настроек клиента.
        writer.Write(new byte[9]);
        ReadOnlySpan<byte> quickbar = character.Quickbar;
        WriteUInt16(writer, checked((ushort)quickbar.Length));
        writer.Write(quickbar);
        writer.Write(new byte[26]);
        WriteInt32(writer, tick);
    }

    private static void WriteInitialState(BinaryWriter writer, WorldCharacter character)
    {
        WriteInt32(writer, character.Id);
        LoginPackets.WriteString(writer, character.Name);

        // Одинаковый блок начальных флагов после имени в обоих успешных входах.
        byte[] state = new byte[150];
        ReadOnlySpan<int> enabledOffsets = [2, 5, 27, 33, 40, 48, 57, 77, 85, 94, 146];
        foreach (int offset in enabledOffsets)
            state[offset] = 1;
        writer.Write(state);
    }

    private static IEnumerable<byte[]> CreateBackpackLayout(int tick)
    {
        // Клиент размещает предметы только в разделах, которые уже получил в 0x0C78.
        yield return CreateBackpackPage(0, 0, 1, tick);
        yield return CreateBackpackPage(1, 2, 80, tick);
        yield return CreateBackpackPage(FirstBackpackPage, 1, BackpackPageSize, tick);
        for (byte page = FirstBackpackPage; page < FirstBackpackPage + BackpackPageCount; page++)
            yield return CreateBackpackPage(page, 3, BackpackPageSize, tick);
    }

    private static byte[] CreateBackpackPage(byte page, byte layout, ushort slots, int tick) =>
        Build(Opcode.BackpackLayout, writer =>
        {
            writer.Write((byte)0);
            writer.Write(page);
            writer.Write(layout);
            WriteUInt16(writer, slots);
            WriteInt32(writer, tick);
        });

    internal static byte[] CreateInventory(int characterId, int silver, IEnumerable<CharacterItem> inventory, int tick)
    {
        var items = inventory.Where(item => item.BackpackIndex < BackpackPageSize * BackpackPageCount)
            .ToDictionary(item => item.BackpackIndex);
        return CreateBackpackUpdate(characterId, silver, Enumerable.Range(0, BackpackPageSize * BackpackPageCount)
            .Select(index => ((ushort)index, items.GetValueOrDefault((ushort)index))), tick);
    }

    internal static byte[] CreateBackpackUpdate(int characterId, int silver, params (ushort Index, CharacterItem? Item)[] updates) =>
        CreateBackpackUpdate(characterId, silver, updates, Environment.TickCount);

    private static byte[] CreateBackpackUpdate(int characterId, int silver,
        IEnumerable<(ushort Index, CharacterItem? Item)> updates, int tick)
    {
        var pages = updates.ToLookup(update => FirstBackpackPage + update.Index / BackpackPageSize);
        int pageCount = pages.Select(page => page.Key + 1).DefaultIfEmpty().Max();
        return Build(Opcode.InitialInventory, writer =>
        {
            WriteInt32(writer, silver);
            writer.Write((byte)pageCount);
            for (int bag = 0; bag < pageCount; bag++)
            {
                var slots = pages[bag];
                WriteUInt16(writer, (ushort)slots.Count());
                foreach (var (index, item) in slots)
                    WriteInventorySlot(writer, characterId, (ushort)(index % BackpackPageSize), item);
            }
            WriteInt32(writer, tick);
        });
    }

    private static void WriteInventorySlot(BinaryWriter writer, int characterId, ushort slot, CharacterItem? item)
    {
        WriteUInt16(writer, slot);
        ushort itemId = item?.ItemId ?? 0;
        byte dragonType = DragonSlotType(itemId);
        if (item?.EquipmentSlot is byte && item.BackpackSlot.HasValue)
        {
            WriteEquipmentInventorySlot(writer, characterId, item);
        }
        else if (dragonType == 0)
        {
            writer.Write((byte)0);
            WriteInt32(writer, item is null ? 0 : (item.Quantity << 16) | itemId);
        }
        else
        {
            // Числовые поля 36-байтовой записи экземпляра идут в little-endian.
            // Номер экземпляра после записи передаётся отдельно в big-endian.
            bool battleDragon = dragonType == BattleDragonType;
            int instanceId = DragonItemInstanceId(characterId, item!.Slot);
            writer.Write((byte)1);
            WriteUInt16(writer, 36);
            writer.Write(characterId);
            writer.Write(instanceId);
            writer.Write(itemId);
            writer.Write(new byte[10]);
            writer.Write(battleDragon ? 0 : item!.DragonGrowth);
            writer.Write((ushort)(battleDragon ? 15 : 100));
            writer.Write((ushort)(battleDragon ? 100 : item!.DragonSatiety));
            writer.Write((ushort)(battleDragon ? 100 : 0));
            writer.Write((ushort)0);
            writer.Write((ushort)100);
            writer.Write((ushort)(battleDragon ? BattleDragonType : 0));
            WriteInt32(writer, instanceId);
        }

        writer.Write((byte)0);
    }

    private static void WriteEquipmentInventorySlot(BinaryWriter writer, int characterId, CharacterItem item)
    {
        int instanceId = EquipmentInstanceId(characterId, item.EquipmentSlot!.Value);
        writer.Write((byte)1);
        WriteUInt16(writer, 36);
        writer.Write(BitConverter.GetBytes(characterId));
        writer.Write(BitConverter.GetBytes(instanceId));
        writer.Write(BitConverter.GetBytes((int)item.ItemId));
        writer.Write(new byte[18]);
        writer.Write(new byte[] { 2, 0, 0x88, 0x13, 0x04, 0 });
        WriteInt32(writer, instanceId);
        writer.Write((byte)0);
    }

    private static int DragonItemInstanceId(int characterId, ushort slot) =>
        0x70000000 | ((characterId & 0x003FFFFF) << 6) | slot;

    public static byte DragonSlotType(ushort itemId) => itemId == CharacterRepository.ElvenDragonItem
        ? BattleDragonType
        : itemId == CharacterRepository.YoungSilverDragonItem
            || itemId == CharacterRepository.StarterDragonEgg
            || CharacterRepository.HatchlingDragonItems.Contains(itemId)
            ? GrowingDragonType : (byte)0;

    public static byte[] CreateDragonSlots(int characterId, IEnumerable<CharacterItem> inventory, ushort? activeBattleItem = null) =>
        Build(Opcode.DragonSlots, writer =>
        {
            var slotted = inventory.Where(item => (item.DragonSlot.HasValue || item.Slot == activeBattleItem)
                && DragonSlotType(item.ItemId) != 0).ToArray();
            writer.Write(checked((byte)slotted.Length));
            foreach (var item in slotted)
            {
                WriteInt32(writer, DragonItemInstanceId(characterId, item.Slot));
                writer.Write(item.Slot == activeBattleItem ? ActiveBattleDragonSlot : item.DragonSlot!.Value);
                writer.Write(DragonSlotType(item.ItemId));
            }
        });

    internal static byte[] CreateEquipmentList(WorldCharacter character,
        IReadOnlyList<CharacterItem> equipment, int tick) =>
        Build(Opcode.EquipmentList, writer => WriteEquipment(writer, character, equipment, tick));

    internal static byte[] CreateEquipmentSlotFlags() => Build(Opcode.EquipmentSlotFlags, writer =>
    {
        WriteUInt16(writer, 5);
        for (ushort slot = 0; slot < 5; slot++)
        {
            WriteUInt16(writer, slot);
            writer.Write(new byte[6]);
        }
    });

    internal static byte[] CreateEquipmentItemList(int characterId,
        IReadOnlyList<CharacterItem> equipment) => Build(Opcode.EquipmentItemList, writer =>
    {
        WriteInt32(writer, characterId);
        WriteUInt16(writer, checked((ushort)equipment.Count));
        writer.Write(new byte[] { 1, 1 });
        foreach (var item in equipment.OrderBy(item => item.EquipmentSlot))
        {
            WriteUInt16(writer, item.EquipmentSlot!.Value);
            WriteInt32(writer, item.ItemId);
            WriteInt32(writer, 0);
        }
    });

    private static void WriteEquipment(BinaryWriter writer, WorldCharacter character,
        IReadOnlyList<CharacterItem> equipment, int tick)
    {
        WriteInt32(writer, character.Id);
        WriteUInt16(writer, 16);
        for (ushort slot = 0; slot < 16; slot++)
        {
            WriteUInt16(writer, slot);
            WriteUInt16(writer, equipment.FirstOrDefault(item => item.EquipmentSlot == slot)?.ItemId ?? (ushort)0);
            if (slot >= 14)
                WriteUInt16(writer, 0);
        }
        if (character.Appearance is { Length: LoginPackets.AppearanceSize } appearance)
            writer.Write(appearance.AsSpan(LoginPackets.AppearanceSize - 8, 6));
        else
            writer.Write(new byte[6]);
        writer.Write((byte)0);
        WriteInt32(writer, tick);
    }

    public static byte[] CreateLearnedSkills(int actorId, IReadOnlyList<ushort> skills) =>
        Build(Opcode.LearnedSkills, writer =>
        {
            WriteInt32(writer, actorId);
            WriteUInt16(writer, checked((ushort)skills.Count));
            foreach (ushort skill in skills)
            {
                WriteUInt16(writer, skill);
                WriteUInt16(writer, 1);
                WriteInt32(writer, 0);
            }
            WriteInt32(writer, Environment.TickCount);
        });

    public static byte[] CreateDragonSpawn(int actorId, (int X, int Y, int Z) position, bool mounted,
        ushort itemId = CharacterRepository.ElvenDragonItem) =>
        Build(Opcode.PlayerSpawn, writer =>
        {
            var dragonPosition = mounted ? position : (position.X + 175, position.Y + 125, position.Z);
            var appearance = YoungDragonAppearances.TryGetValue(itemId, out var selected)
                ? selected : ((ushort)7508, "Молодой Серебряный дракон");
            WriteActorAppearance(writer, actorId, 0x25, dragonPosition, 0, appearance.Item1);
            if (mounted)
                WriteBattleDragonAttributes(writer, itemId);
            else
            {
                bool youngDragon = CharacterRepository.HatchlingDragonItems.Contains(itemId)
                    || itemId == CharacterRepository.YoungSilverDragonItem;
                WriteActorVitals(writer, youngDragon ? 10 : 15,
                    youngDragon ? 3000 : 300,
                    youngDragon ? 3000 : 300,
                    youngDragon ? 100 : 0);
            }

            WriteNamedSpawnTail(writer, appearance.Item2, Environment.TickCount);
        });

    public static byte[] CreateDragonLink(int characterId, int dragonId, bool attach = true) =>
        Build(attach ? Opcode.DragonLink : Opcode.DragonUnlink, writer =>
        {
            WriteInt32(writer, characterId);
            WriteInt32(writer, dragonId);
            writer.Write(Environment.TickCount);
        });

    public static byte[] CreateDragonAttributes(int dragonId, ushort itemId = CharacterRepository.ElvenDragonItem) =>
        Build(Opcode.DragonAttributes, writer =>
        {
            WriteInt32(writer, dragonId);
            WriteBattleDragonAttributes(writer, itemId);
            writer.Write(new byte[6]);
            writer.Write(Environment.TickCount);
        });

    private static readonly byte[] EquipmentDetailsTemplate = Convert.FromHexString(
        "00f1000000000d9600000202001000000000010100240202000021200060c15d00000000000000000000000000000000000000000200881304006000202100020100240202000022200060a961000000000000000000000000000000000000000002008813040060002022000301002402020000232000607969000000000000000000000000000000000000000002008813040060002023000401002402020000242000609165000000000000000000000000000000000000000002008813040060002024000500000600000700000800000900000a00000b00000c00000d0000000e00000000000f00000000f4e05ebe");

    internal static byte[] CreateEquipmentDetails(int characterId, IReadOnlyList<CharacterItem> equipment, int tick)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(new byte[LoginPackets.HeaderSize]);
        WriteInt32(writer, characterId);
        WriteUInt16(writer, 16);
        writer.Write(new byte[4]);
        for (byte slot = 1; slot <= 4; slot++)
        {
            writer.Write(slot);
            var item = equipment.FirstOrDefault(candidate => candidate.EquipmentSlot == slot);
            if (item is null)
            {
                writer.Write((byte)0);
                writer.Write((byte)0);
                continue;
            }

            int instance = EquipmentInstanceId(characterId, slot);
            writer.Write((byte)1);
            WriteUInt16(writer, 36);
            writer.Write(BitConverter.GetBytes(characterId));
            writer.Write(BitConverter.GetBytes(instance));
            writer.Write(BitConverter.GetBytes((int)item.ItemId));
            writer.Write(new byte[18]);
            writer.Write(new byte[] { 2, 0, 0x88, 0x13, 0x04, 0 });
            WriteInt32(writer, instance);
            writer.Write((byte)0);
        }

        writer.Write(EquipmentDetailsTemplate.AsSpan(198));
        byte[] packet = stream.ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(packet.Length - sizeof(int)), tick);
        LoginPackets.WriteHeader(packet, Opcode.EquipmentDetails);
        return packet;
    }

    private static int EquipmentInstanceId(int characterId, byte equipmentSlot) =>
        0x60000000 | ((characterId & 0x0FFFFFFF) << 4) | equipmentSlot;

    public static byte[] CreateDragonSlotResult(byte bag, ushort slot, byte dragonSlot, bool remove, byte itemType,
        ushort result = 0) =>
        Build(Opcode.DragonSlotResult, writer =>
        {
            writer.Write(bag);
            WriteUInt16(writer, slot);
            writer.Write(dragonSlot);
            writer.Write(remove);
            writer.Write(itemType);
            WriteUInt16(writer, result);
        });

    internal static byte[] CreateHatchOptions(int characterId, CharacterItem egg, ushort? sourceItem = null)
    {
        ushort[] choices = [58503, 58505, 58506, 58507, 58508, 58509];
        return Build(Opcode.Unknown0D34, writer =>
        {
            WriteInt32(writer, DragonItemInstanceId(characterId, egg.Slot));
            WriteInt32(writer, characterId);
            WriteInt32(writer, sourceItem ?? CharacterRepository.StarterDragonEgg);
            WriteUInt16(writer, checked((ushort)choices.Length));
            foreach (ushort choice in choices)
            {
                WriteInt32(writer, choice);
                WriteInt32(writer, 100);
            }
        });
    }

    internal static byte[] CreateDragonEvolutionWindow(int characterId, CharacterItem dragon)
        => Build(Opcode.Unknown0D34, writer =>
        {
            // The client expects the two evolution-window actor identifiers
            // used by the captured build before it accepts the 0x0943 reply.
            WriteInt32(writer, unchecked((int)0x7000B78E));
            WriteInt32(writer, unchecked((int)0x29A41676));
            WriteInt32(writer, dragon.ItemId);
            writer.Write(new byte[] { 0, 1, 0, 0, 0xA0, 0x3B, 0, 0, 0, 0x64 });
        });

    internal static byte[] CreateDragonEvolutionResult(int characterId, CharacterItem dragon)
        => Build(Opcode.Unknown0D35, writer =>
        {
            WriteInt32(writer, unchecked((int)0x7000B78E));
            WriteUInt16(writer, 0);
        });

    internal static byte[] RefreshDragonItem(CharacterItem item)
    {
        int index = item.BackpackIndex;
        // При ненулевом результате клиент разблокирует существующую иконку 0x0FB6 без пересоздания предмета.
        return CreateDragonSlotResult((byte)(FirstBackpackPage + index / BackpackPageSize),
            (ushort)(index % BackpackPageSize), 0, false, DragonSlotType(item.ItemId), result: 1);
    }

    public static byte[] CreateDespawn(int actorId) =>
        Build(Opcode.Despawn, writer =>
        {
            WriteInt32(writer, actorId);
            writer.Write(Environment.TickCount);
        });

    internal static byte[] CreateNpcSpawn(WorldNpc npc) =>
        Build(Opcode.PlayerSpawn, writer =>
        {
            WriteActorAppearance(writer, npc.Id, 0x87, (npc.X, npc.Y, npc.Z), 0, npc.Template);
            WriteActorVitals(writer, 1, 100, 100, 50);
            LoginPackets.WriteString(writer, npc.Name);
            writer.Write(new byte[31]);
        });

    internal static byte[] CreateCreatureSpawn(CreatureSpawn spawn, int health) =>
        Build(Opcode.PlayerSpawn, writer =>
        {
            string? airborneName = spawn.Template.AirborneName;
            bool airborne = airborneName is not null;
            WriteActorAppearance(writer, spawn.Id, airborne ? 0x26 : 0x47, spawn.Position,
                airborne ? (ushort)0 : (ushort)90, spawn.Template.Id, airborne);
            WriteActorVitals(writer, spawn.Template.Level, health, spawn.Template.HitPoints, spawn.Template.Mana);
            if (airborneName is not null)
                WriteNamedSpawnTail(writer, airborneName, Environment.TickCount);
            else
                writer.Write(new byte[6]);
        });

    public static byte[] CreateBattleDragonCast(int characterId, (int X, int Y, int Z) position, byte phase) =>
        CreateSkillAction(characterId, characterId, position, BattleDragonSummonSkill, phase,
            Environment.TickCount, phase == 0xFF ? BattleDragonSummonTimeMs : 0);

    public static byte[][] CreateHit(int attackerId, CreatureSpawn target, ushort skill, int damage, int remainingHealth)
    {
        int tick = Environment.TickCount;
        byte[][] packets =
        [
            CreateSkillAction(attackerId, target.Id, target.Position, skill, 0xFF, tick, 1),
            CreateSkillAction(attackerId, target.Id, target.Position, skill, 0xFE, tick, 0),
            CreateSkillAction(attackerId, target.Id, target.Position, skill, 0xFD, tick, 0),
            Build(Opcode.SkillHit, writer =>
            {
                WriteInt32(writer, target.Id);
                writer.Write((byte)0x0A);
                WriteUInt16(writer, skill);
                WriteInt32(writer, attackerId);
                WritePosition(writer, target.Position);
                WriteInt32(writer, damage);
                writer.Write(new byte[9]);
                WriteInt32(writer, tick);
            }),
            Build(Opcode.AttributeChange, writer =>
            {
                WriteInt32(writer, target.Id);
                WriteUInt16(writer, 2);
                WriteAttribute(writer, 0x0D, remainingHealth);
                WriteAttribute(writer, 0x0F, target.Template.HitPoints);
                writer.Write(tick);
            })
        ];
        return remainingHealth == 0
            ? [.. packets, Build(Opcode.ActorDeath, writer =>
            {
                WriteInt32(writer, target.Id);
                WriteInt32(writer, attackerId);
                WriteInt32(writer, tick);
            })]
            : packets;
    }

    private static byte[] CreateSkillAction(int attackerId, int targetId, (int X, int Y, int Z) position,
        ushort skill, byte phase, int tick, int castTime) =>
        Build(Opcode.SkillAction, writer =>
        {
            writer.Write(phase);
            WriteInt32(writer, attackerId);
            WriteUInt16(writer, skill);
            WriteInt32(writer, castTime);
            writer.Write((byte)1);
            WriteInt32(writer, targetId);
            WritePosition(writer, position);
            WriteInt32(writer, tick);
        });

    private static void WriteActorAppearance(BinaryWriter writer, int id, int flags,
        (int X, int Y, int Z) position, ushort heading, ushort template, bool airborne = false)
    {
        WriteInt32(writer, id);
        WriteInt32(writer, flags);
        WritePosition(writer, position);
        WriteUInt16(writer, heading);
        Span<byte> state = stackalloc byte[40];
        state.Clear();
        if (airborne)
        {
            // В захватах воздушные актёры 0x26 имеют эти два флага состояния.
            state[9] = 0xFA;
            state[10] = 1;
        }
        writer.Write(state);
        WriteUInt16(writer, template);
        WriteUInt16(writer, 0);
    }

    private static void WriteActorVitals(BinaryWriter writer, int level, int health, int maximumHealth, int mana)
    {
        WriteUInt16(writer, 5);
        WriteAttribute(writer, 0, level);
        WriteAttribute(writer, 0x0D, health);
        WriteAttribute(writer, 0x0F, maximumHealth);
        WriteAttribute(writer, 0x0E, mana);
        WriteAttribute(writer, 0x10, mana);
    }

    private static void WriteNamedSpawnTail(BinaryWriter writer, string name, int tick)
    {
        writer.Write(new byte[11]);
        LoginPackets.WriteString(writer, name);
        Span<byte> actorFlags = stackalloc byte[40];
        actorFlags.Clear();
        actorFlags[1] = 1;
        actorFlags[4] = 1;
        writer.Write(actorFlags);
        WriteInt32(writer, tick);
    }

    private static void WriteBattleDragonAttributes(BinaryWriter writer, ushort itemId)
    {
        var attributes = CharacterRepository.DragonContractItems.Contains(itemId)
            ? YoungDragonAttributes : BattleDragonAttributes;
        WriteUInt16(writer, (ushort)attributes.Length);
        foreach (var (id, value) in attributes)
            WriteAttribute(writer, id, value);
    }

    private static byte[] Build(Opcode opcode, Action<BinaryWriter>? writeBody = null)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(new byte[LoginPackets.HeaderSize]);
        writeBody?.Invoke(writer);
        byte[] packet = stream.ToArray();
        LoginPackets.WriteHeader(packet, opcode);
        return packet;
    }

    private static void WritePosition(BinaryWriter writer, (int X, int Y, int Z) position)
    {
        WriteInt32(writer, position.X);
        WriteInt32(writer, position.Y);
        WriteInt32(writer, position.Z);
    }

    private static void WriteAttribute(BinaryWriter writer, ushort id, int value)
    {
        WriteUInt16(writer, id);
        WriteInt32(writer, value);
    }

    private static void WriteVitalAttributes(BinaryWriter writer, WorldCharacter character)
    {
        WriteAttribute(writer, 0x0D, character.HitPoints);
        WriteAttribute(writer, 0x0F, character.HitPoints);
        WriteAttribute(writer, 0x0E, character.Mana);
        WriteAttribute(writer, 0x10, character.Mana);
    }

    private static void WriteUInt16(BinaryWriter writer, ushort value) =>
        writer.Write(BinaryPrimitives.ReverseEndianness(value));

    private static void WriteInt32(BinaryWriter writer, int value) =>
        writer.Write(BinaryPrimitives.ReverseEndianness(value));
}
