using System.Buffers.Binary;
using System.Globalization;
using DragonKnightServer.Database;

namespace DragonKnightServer.Protocol;

internal static class WorldPackets
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
    private static readonly (ushort Id, int Value)[] BattleDragonAttributes =
    [
        (0, 15), (0x0D, 3000), (0x0F, 3000), (0x0E, 1500), (0x10, 1500), (0x0C, 100),
        (0x11, 30), (0x12, 30), (0x13, 150), (0x14, 30), (0x15, 150), (0x16, 20),
        (0x17, 100), (0x18, 150), (0x19, 20), (0x1A, 150), (0x1B, 100), (0x1C, 20),
        (0x1D, 150), (0x1E, 150), (0x1F, 100), (0x20, 15), (0x21, 15), (0x22, 15),
        (0x23, 15), (0x24, ElvenDragonFlightSpeed), (0x25, 0), (0x26, 0), (0x27, 0), (0x28, 0),
        (0x29, 0), (0x2A, 0), (0x2B, 0), (0x2C, 0), (0x2D, 0)
    ];

    public static byte[][] CreateEntry(WorldCharacter character)
    {
        var position = StartingPosition(character);
        int tick = Environment.TickCount;
        var equipment = StarterEquipment.ForTemplate(character.Template);
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
            Build(Opcode.PlayerAttributes, writer =>
            {
                WriteInt32(writer, character.Id);
                WriteUInt16(writer, 5);
                WriteVitalAttributes(writer, character);
                WriteAttribute(writer, 0x24, WalkingSpeed);
                // В 0x0D92 отметка времени записана в little-endian.
                writer.Write(tick);
            }),
            Build(Opcode.InitialPlayerState, writer => WriteInitialState(writer, character)),
            .. CreateBackpackLayout(tick),
            CreateInventory(character.Id, character.Inventory, unchecked(tick + 1)),
            CreateDragonSlots(character.Id, character.Inventory),
            Build(Opcode.EquipmentList, writer => WriteEquipment(writer, character, equipment, tick)),
            CreateLearnedSkills(0, WorldSession.GetPlayerSkills(character.Template)),
            Build(Opcode.WorldEntryStatus, writer => WriteUInt16(writer, 0)),
            Build(Opcode.WorldEntryComplete)
        ];
    }

    public static byte[] CreateClock() => Build(Opcode.WorldClock, writer =>
    {
        WriteInt32(writer, 0);
        WriteInt32(writer, Environment.TickCount);
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
        // (100, 200, 0) — значения заглушки схемы для новых персонажей.
        // Для map001 используем проверенную точку появления на существующей клиентской карте.
        return character.Map == 1 && (character.X, character.Y, character.Z) == (100f, 200f, 0f)
            ? (452383, 159581, 1557)
            : ((int)character.X, (int)character.Y, (int)character.Z);
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
        WriteUInt16(writer, 1);

        WriteUInt16(writer, 5);
        WriteAttribute(writer, 0, character.Level);
        WriteVitalAttributes(writer, character);
        WriteUInt16(writer, 0);
        writer.Write((byte)equipment.Count);
        foreach (var item in equipment)
        {
            WriteUInt16(writer, item.Slot);
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

        (ushort Id, int Value)[] attributes =
        [
            // Базовые характеристики персонажа 1-го уровня из клиентской таблицы Chainit.
            (0, character.Level), (0x11, 3), (0x12, 5), (0x13, 2),
            (0x14, 10), (0x15, 100), (0x16, 1), (0x17, 0), (0x18, character.Experience),
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

    private static byte[] CreateInventory(int characterId, IEnumerable<CharacterItem> inventory, int tick)
    {
        var items = inventory.ToDictionary(item => item.BackpackIndex);
        return CreateBackpackUpdate(characterId, Enumerable.Range(0, BackpackPageSize * BackpackPageCount)
            .Select(index => ((ushort)index, items.GetValueOrDefault((ushort)index))), tick);
    }

    internal static byte[] CreateBackpackUpdate(int characterId, params (ushort Index, CharacterItem? Item)[] updates) =>
        CreateBackpackUpdate(characterId, updates, Environment.TickCount);

    private static byte[] CreateBackpackUpdate(int characterId,
        IEnumerable<(ushort Index, CharacterItem? Item)> updates, int tick)
    {
        var pages = updates.ToLookup(update => FirstBackpackPage + update.Index / BackpackPageSize);
        int pageCount = pages.Max(page => page.Key) + 1;
        return Build(Opcode.InitialInventory, writer =>
        {
            WriteInt32(writer, 0);
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
        if (dragonType == 0)
        {
            writer.Write((byte)0);
            WriteInt32(writer, itemId == 0 ? 0 : 0x10000 | itemId);
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
            writer.Write(battleDragon ? 0 : 100);
            writer.Write((ushort)(battleDragon ? 15 : 100));
            writer.Write((ushort)100);
            writer.Write((ushort)(battleDragon ? 100 : 0));
            writer.Write((ushort)0);
            writer.Write((ushort)100);
            writer.Write((ushort)(battleDragon ? BattleDragonType : 0));
            WriteInt32(writer, instanceId);
        }

        writer.Write((byte)0);
    }

    private static int DragonItemInstanceId(int characterId, ushort slot) =>
        0x70000000 | ((characterId & 0x003FFFFF) << 6) | slot;

    public static byte DragonSlotType(ushort itemId) => itemId switch
    {
        CharacterRepository.YoungSilverDragonItem => GrowingDragonType,
        CharacterRepository.ElvenDragonItem => BattleDragonType,
        _ => 0
    };

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

    private static void WriteEquipment(BinaryWriter writer, WorldCharacter character,
        IReadOnlyList<CharacterItem> equipment, int tick)
    {
        WriteInt32(writer, character.Id);
        WriteUInt16(writer, 16);
        for (ushort slot = 0; slot < 16; slot++)
        {
            WriteUInt16(writer, slot);
            WriteUInt16(writer, equipment.FirstOrDefault(item => item.Slot == slot)?.ItemId ?? (ushort)0);
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

    public static byte[] CreateDragonSpawn(int actorId, (int X, int Y, int Z) position, bool mounted) =>
        Build(Opcode.PlayerSpawn, writer =>
        {
            var dragonPosition = mounted ? position : (position.X + 175, position.Y + 125, position.Z);
            WriteActorAppearance(writer, actorId, 0x25, dragonPosition, 0, mounted ? (ushort)6018 : (ushort)7508);
            if (mounted)
                WriteBattleDragonAttributes(writer);
            else
                WriteActorVitals(writer, 15, 300, 300, 0);

            writer.Write(new byte[11]);
            LoginPackets.WriteString(writer, mounted ? "Эльфийский дракон" : "Молодой Серебряный дракон");
            byte[] actorFlags = new byte[40];
            actorFlags[1] = 1;
            actorFlags[4] = 1;
            writer.Write(actorFlags);
            WriteInt32(writer, Environment.TickCount);
        });

    public static byte[] CreateDragonLink(int characterId, int dragonId, bool attach = true) =>
        Build(attach ? Opcode.DragonLink : Opcode.DragonUnlink, writer =>
        {
            WriteInt32(writer, characterId);
            WriteInt32(writer, dragonId);
            writer.Write(Environment.TickCount);
        });

    public static byte[] CreateDragonAttributes(int dragonId) =>
        Build(Opcode.DragonAttributes, writer =>
        {
            WriteInt32(writer, dragonId);
            WriteBattleDragonAttributes(writer);
            writer.Write(new byte[6]);
            writer.Write(Environment.TickCount);
        });

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

    internal static byte[] CreateAllianceNpcSpawn(AllianceNpc npc) =>
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
            WriteActorAppearance(writer, spawn.Id, 0x47, spawn.Position, 90, spawn.Template.Id);
            WriteActorVitals(writer, spawn.Template.Level, health, spawn.Template.HitPoints, spawn.Template.Mana);
            // Для короткой формы имя и модель берутся из шаблона клиента.
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
        (int X, int Y, int Z) position, ushort heading, ushort template)
    {
        WriteInt32(writer, id);
        WriteInt32(writer, flags);
        WritePosition(writer, position);
        WriteUInt16(writer, heading);
        writer.Write(new byte[40]);
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

    private static void WriteBattleDragonAttributes(BinaryWriter writer)
    {
        WriteUInt16(writer, (ushort)BattleDragonAttributes.Length);
        foreach (var (id, value) in BattleDragonAttributes)
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
