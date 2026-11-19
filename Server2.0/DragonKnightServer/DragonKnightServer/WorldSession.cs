using System.Buffers.Binary;
using DragonKnightServer.Database;
using DragonKnightServer.Protocol;

namespace DragonKnightServer;

internal sealed class WorldSession(WorldCharacter character)
{
    private const int SpawnDistance = 12_000;
    private const int DespawnDistance = 15_000;
    private (int X, int Y, int Z) position = WorldPackets.StartingPosition(character);
    private readonly Dictionary<int, CreatureSpawn> visibleCreatures = [];
    private readonly Dictionary<int, int> injuredCreatures = [];
    private readonly Dictionary<int, (CreatureSpawn Spawn, long CorpseUntil, long RespawnAt)> defeatedCreatures = [];
    private readonly IReadOnlyList<AllianceNpc> allianceNpcs = AlliancePopulation.ForMap(character.Map);
    private readonly HashSet<int> visibleAllianceNpcs = [];
    private readonly Dictionary<ushort, CharacterItem> inventory = character.Inventory.ToDictionary(item => item.Slot);
    private byte[]? quickbar = character.Quickbar;
    private CharacterItem? companion;
    private ushort? pendingBattleDragonSlot;
    private bool positionChanged;
    private bool moving;
    private int DragonId => (IsMounted ? 0x007A0000 : 0x007B0000) + character.Id;
    private bool IsMounted => companion?.ItemId == CharacterRepository.ElvenDragonItem;
    internal Task? BattleDragonCastDelay { get; private set; }

    internal static ushort[] GetPlayerSkills(int template) => template switch
    {
        1 or 6 => [1, 3061, 1301, 1341],
        2 or 7 => [1, 3061, 1921, 1741],
        3 or 8 => [1, 3021, 881, 901, 1201],
        4 or 9 => [1, 3081, 21, 121, 141],
        5 or 10 => [1, 3081, 461, 481],
        _ => [1]
    };

    internal byte[][] Move(ReadOnlySpan<byte> packet)
    {
        var nextPosition = (BinaryPrimitives.ReadInt32BigEndian(packet[8..]),
            BinaryPrimitives.ReadInt32BigEndian(packet[12..]),
            BinaryPrimitives.ReadInt32BigEndian(packet[16..]));
        positionChanged |= position != nextPosition;
        position = nextPosition;
        ushort mode = BinaryPrimitives.ReadUInt16BigEndian(packet[21..]);
        moving = mode is not (0 or 1);
        var update = WorldPackets.CreatePositionUpdate(IsMounted ? DragonId : character.Id, position, mode, mounted: IsMounted);
        var actorUpdates = UpdateNearbyWorldActors();
        return companion is not null && !IsMounted
            ? [update, WorldPackets.CreatePositionUpdate(DragonId, (position.X + 175, position.Y + 125, position.Z), mode), .. actorUpdates]
            : [update, .. actorUpdates];
    }

    internal IReadOnlyList<byte[]> UpdateNearbyWorldActors()
    {
        var packets = new List<byte[]>();
        foreach (var npc in allianceNpcs)
        {
            bool visible = visibleAllianceNpcs.Contains(npc.Id);
            bool nearby = WithinDistance(npc.X, npc.Y, visible ? DespawnDistance : SpawnDistance);
            if (nearby == visible)
                continue;

            if (nearby)
            {
                visibleAllianceNpcs.Add(npc.Id);
                packets.Add(WorldPackets.CreateAllianceNpcSpawn(npc));
            }
            else
            {
                visibleAllianceNpcs.Remove(npc.Id);
                packets.Add(WorldPackets.CreateDespawn(npc.Id));
            }
        }

        var outOfRange = visibleCreatures.Values
            .Where(spawn => !WithinDistance(spawn.Position.X, spawn.Position.Y, DespawnDistance)).ToArray();
        foreach (var spawn in outOfRange)
        {
            visibleCreatures.Remove(spawn.Id);
            packets.Add(WorldPackets.CreateDespawn(spawn.Id));
        }

        foreach (var spawn in CreaturePopulation.Near(character.Map, position.X, position.Y, SpawnDistance))
        {
            if (!visibleCreatures.ContainsKey(spawn.Id) && !defeatedCreatures.ContainsKey(spawn.Id)
                && WithinDistance(spawn.Position.X, spawn.Position.Y, SpawnDistance))
                packets.Add(ShowCreature(spawn));
        }
        return packets;
    }

    private byte[] ShowCreature(CreatureSpawn spawn)
    {
        visibleCreatures.Add(spawn.Id, spawn);
        return WorldPackets.CreateCreatureSpawn(spawn, injuredCreatures.GetValueOrDefault(spawn.Id, spawn.Template.HitPoints));
    }

    private bool WithinDistance(int x, int y, int distance)
    {
        long dx = (long)position.X - x;
        long dy = (long)position.Y - y;
        return Math.Abs(dx) <= distance && Math.Abs(dy) <= distance
            && dx * dx + dy * dy <= (long)distance * distance;
    }

    internal void SavePosition(int accountId)
    {
        if (positionChanged && CharacterRepository.TrySavePosition(accountId, character.Id, character.Map, position))
            positionChanged = false;
    }

    internal byte[][] Stop(ReadOnlySpan<byte> packet)
    {
        if (!moving || (packet.Length == 13 ? packet[8] != 1 : packet[8..11].IndexOfAnyExcept((byte)0) >= 0))
            return [];

        moving = false;
        var idle = WorldPackets.CreatePositionUpdate(IsMounted ? DragonId : character.Id, position, 1, standing: true);
        return companion is not null && !IsMounted
            ? [idle, WorldPackets.CreatePositionUpdate(DragonId, (position.X + 175, position.Y + 125, position.Z), 1, standing: true)]
            : [idle];
    }

    internal byte[] GetDragonSlots() =>
        WorldPackets.CreateDragonSlots(character.Id, inventory.Values, IsMounted ? companion!.Slot : null);

    internal byte[][] MoveBackpackItem(ReadOnlySpan<byte> packet)
    {
        ushort? firstIndex = ReadBackpackIndex(packet[8], BinaryPrimitives.ReadUInt16BigEndian(packet[9..]));
        ushort? secondIndex = ReadBackpackIndex(packet[11], BinaryPrimitives.ReadUInt16BigEndian(packet[12..]));
        ushort quantity = BinaryPrimitives.ReadUInt16BigEndian(packet[14..]);
        if (firstIndex is not ushort first || secondIndex is not ushort second || first == second || quantity > 1)
            return [];

        // Сетка отправляет сначала ячейку под курсором, а вкладка — сначала ячейку предмета.
        bool samePage = packet[8] == packet[11];
        var (from, to) = samePage ? (second, first) : (first, second);
        var source = FindBackpackItem(from);
        var target = FindBackpackItem(to);
        if (!samePage && source is null)
        {
            (source, target) = (target, source);
            (from, to) = (to, from);
        }
        if (source is null || !CharacterRepository.TryMoveBackpackItem(character.Id, source, to, target))
            return [];

        var moved = source with { BackpackSlot = to };
        var swapped = target is null ? null : target with { BackpackSlot = from };
        inventory[moved.Slot] = moved;
        if (swapped is not null)
            inventory[swapped.Slot] = swapped;
        if (companion is not null)
            companion = inventory[companion.Slot];

        return [WorldPackets.CreateBackpackUpdate(character.Id, (from, swapped), (to, moved)), GetDragonSlots()];
    }

    internal void SaveQuickbar(ReadOnlySpan<byte> packet)
    {
        int dataOffset = LoginPackets.HeaderSize + sizeof(ushort);
        if (packet.Length < dataOffset + sizeof(ushort))
            return;

        int length = BinaryPrimitives.ReadUInt16BigEndian(packet[LoginPackets.HeaderSize..]);
        if (length is < 1 or > WorldPackets.QuickbarMaxSize || packet.Length != dataOffset + length + sizeof(ushort))
            return;

        var data = packet.Slice(dataOffset, length);
        if (quickbar.AsSpan().SequenceEqual(data))
            return;

        var snapshot = data.ToArray();
        if (CharacterRepository.TrySaveQuickbar(character.Id, snapshot))
            quickbar = snapshot;
    }

    internal byte[][] SetDragonSlot(ReadOnlySpan<byte> packet)
    {
        byte bag = packet[8];
        ushort slot = BinaryPrimitives.ReadUInt16BigEndian(packet[9..]);
        byte destination = packet[11];
        byte action = packet[12];
        bool remove = action == 1;
        var item = FindDragonItem(bag, slot);
        byte itemType = item is null ? (byte)0 : WorldPackets.DragonSlotType(item.ItemId);
        int slotCount = itemType == WorldPackets.BattleDragonType ? 6 : 5;
        bool isActive = item is not null && companion?.Slot == item.Slot;
        bool validRemoval = item?.DragonSlot == destination
            || (isActive && IsMounted && destination == WorldPackets.ActiveBattleDragonSlot);
        if (item is null || destination >= slotCount || action > 1 || (remove && !validRemoval))
            return [WorldPackets.CreateDragonSlotResult(bag, slot, destination, remove, itemType, result: 1)];

        if (!TrySaveDragonSlot(item, remove ? null : destination, out var saved))
            return [WorldPackets.CreateDragonSlotResult(bag, slot, destination, remove, itemType, result: 1)];

        var result = WorldPackets.CreateDragonSlotResult(bag, slot, destination, remove, itemType);
        if (remove && pendingBattleDragonSlot == item.Slot)
            return [result, .. CancelBattleDragonCast(), GetDragonSlots()];
        if (remove && isActive)
            return [result, .. RecallDragon()];
        if (isActive)
            companion = saved;
        return [result, GetDragonSlots()];
    }

    internal byte[][] Call(ReadOnlySpan<byte> packet)
    {
        byte bag = packet[8];
        ushort slot = BinaryPrimitives.ReadUInt16BigEndian(packet[9..]);
        byte action = packet[11];
        // При вызове с земли передаётся один актёр, а при использовании верхом клиент может дописать второй ID.
        if (action > 1 || !IsDragonActionActor(BinaryPrimitives.ReadInt32BigEndian(packet[12..]))
            || (packet.Length == 20 && !IsDragonActionActor(BinaryPrimitives.ReadInt32BigEndian(packet[16..]))))
            return [];

        var item = bag == 0 ? FindAssignedDragon(slot) : FindDragonItem(bag, slot);
        if (item is null)
            return [];
        if (action == 1)
            return UseDragonItem(item);
        if (pendingBattleDragonSlot == item.Slot)
            return CancelBattleDragonCast();
        return companion?.Slot == item.Slot ? RecallDragon() : [];
    }

    internal byte[][] CompleteBattleDragonCast()
    {
        if (pendingBattleDragonSlot is not ushort slot || BattleDragonCastDelay?.IsCompletedSuccessfully != true)
            return [];

        pendingBattleDragonSlot = null;
        BattleDragonCastDelay = null;
        return
        [
            WorldPackets.CreateBattleDragonCast(character.Id, position, 0xFE),
            .. ChangeDragon(inventory[slot]),
            WorldPackets.CreateBattleDragonCast(character.Id, position, 0xFD),
            GetDragonSlots()
        ];
    }

    internal byte[][] UseSkill(ReadOnlySpan<byte> packet)
    {
        if (packet[10] != 1)
            return [];

        ushort skill = BinaryPrimitives.ReadUInt16BigEndian(packet[8..]);
        int actorId = packet.Length == 19 ? BinaryPrimitives.ReadInt32BigEndian(packet[15..]) : character.Id;
        if (skill is WorldPackets.BattleDragonSummonSkill or 28096)
        {
            if (!IsDragonActionActor(actorId))
                return [];
            ushort itemId = skill == WorldPackets.BattleDragonSummonSkill
                ? CharacterRepository.ElvenDragonItem : CharacterRepository.YoungSilverDragonItem;
            var item = inventory.Values.FirstOrDefault(candidate => candidate.ItemId == itemId);
            return item is null ? [] : UseDragonItem(item);
        }

        var skills = IsMounted ? WorldPackets.DragonSkills : GetPlayerSkills(character.Template);
        if (actorId != character.Id || !skills.Contains(skill))
            return [];

        int target = BinaryPrimitives.ReadInt32BigEndian(packet[11..]);
        if (!visibleCreatures.TryGetValue(target, out var creature) || defeatedCreatures.ContainsKey(target))
            return [];

        int remaining = injuredCreatures.GetValueOrDefault(target, creature.Template.HitPoints);
        int damage = Math.Min(remaining, IsMounted ? 28 : 16);
        int newHealth = remaining - damage;
        if (newHealth == 0)
        {
            injuredCreatures.Remove(target);
            long corpseUntil = Environment.TickCount64 + creature.Template.CorpseMs;
            defeatedCreatures.Add(target, (creature, corpseUntil, corpseUntil + creature.Template.RefreshMs));
        }
        else
            injuredCreatures[target] = newHealth;

        return WorldPackets.CreateHit(IsMounted ? DragonId : character.Id, creature, skill, damage, newHealth);
    }

    internal byte[][] CollectCreatureRespawns(long now)
    {
        if (defeatedCreatures.Count == 0)
            return [];

        var packets = new List<byte[]>();
        foreach (var (id, defeated) in defeatedCreatures.ToArray())
        {
            if (defeated.CorpseUntil <= now && visibleCreatures.Remove(id))
                packets.Add(WorldPackets.CreateDespawn(id));

            if (defeated.RespawnAt <= now)
            {
                defeatedCreatures.Remove(id);
                var spawn = defeated.Spawn;
                if (WithinDistance(spawn.Position.X, spawn.Position.Y, SpawnDistance))
                    packets.Add(ShowCreature(spawn));
            }
        }
        return packets.ToArray();
    }

    private static ushort? ReadBackpackIndex(byte bag, ushort slot)
    {
        int page = bag - WorldPackets.FirstBackpackPage;
        return page >= 0 && page < WorldPackets.BackpackPageCount && slot < WorldPackets.BackpackPageSize
            ? (ushort)(page * WorldPackets.BackpackPageSize + slot) : null;
    }

    private CharacterItem? FindBackpackItem(ushort index) =>
        inventory.Values.FirstOrDefault(item => item.BackpackIndex == index);

    private CharacterItem? FindDragonItem(byte bag, ushort slot)
    {
        if (ReadBackpackIndex(bag, slot) is not ushort index)
            return null;
        var item = FindBackpackItem(index);
        return item is not null && WorldPackets.DragonSlotType(item.ItemId) != 0 ? item : null;
    }

    private bool IsDragonActionActor(int actorId) =>
        actorId == character.Id || (companion is not null && actorId == DragonId);

    private CharacterItem? FindAssignedDragon(ushort slotTag)
    {
        // Клиент передаёт индекс ячейки выращивания или тег ячейки боевого дракона (50–55).
        if (slotTag < 5)
            return inventory.Values.FirstOrDefault(item => item.ItemId == CharacterRepository.YoungSilverDragonItem
                && item.DragonSlot == slotTag);
        int battleSlot = slotTag - WorldPackets.FirstBattleDragonSlotTag;
        if (battleSlot < 0 || battleSlot > WorldPackets.ActiveBattleDragonSlot)
            return null;
        if (IsMounted && battleSlot == WorldPackets.ActiveBattleDragonSlot)
            return companion;
        return inventory.Values.FirstOrDefault(item => item.ItemId == CharacterRepository.ElvenDragonItem
            && item.DragonSlot == battleSlot);
    }

    private bool TrySaveDragonSlot(CharacterItem item, byte? slot, out CharacterItem saved)
    {
        saved = item with { DragonSlot = slot };
        if (slot == item.DragonSlot)
            return true;
        if (!CharacterRepository.SaveDragonSlot(character.Id, saved))
            return false;
        inventory[item.Slot] = saved;
        return true;
    }

    private byte[][] UseDragonItem(CharacterItem item)
    {
        if (companion?.Slot == item.Slot)
            return RecallDragon();
        if (pendingBattleDragonSlot == item.Slot)
            return CancelBattleDragonCast();
        if (item.ItemId != CharacterRepository.ElvenDragonItem)
            return [.. CancelBattleDragonCast(), .. ChangeDragon(item), GetDragonSlots()];

        pendingBattleDragonSlot = item.Slot;
        BattleDragonCastDelay = Task.Delay(WorldPackets.BattleDragonSummonTimeMs);
        return [WorldPackets.CreateBattleDragonCast(character.Id, position, 0xFF)];
    }

    private byte[][] RecallDragon() => [.. ChangeDragon(null), GetDragonSlots()];

    private byte[][] CancelBattleDragonCast()
    {
        if (pendingBattleDragonSlot is not ushort slot)
            return [];

        pendingBattleDragonSlot = null;
        BattleDragonCastDelay = null;
        return [WorldPackets.CreateBattleDragonCast(character.Id, position, 0xFD), WorldPackets.RefreshDragonItem(inventory[slot])];
    }

    private byte[][] ChangeDragon(CharacterItem? item)
    {
        if (companion?.Slot == item?.Slot)
            return [];

        var packets = new List<byte[]>();
        if (companion is not null)
        {
            if (IsMounted)
                packets.Add(WorldPackets.CreateDragonLink(character.Id, DragonId, attach: false));
            packets.Add(WorldPackets.CreateDespawn(DragonId));
            packets.Add(WorldPackets.RefreshDragonItem(companion));
        }

        companion = item;
        if (item is not null)
        {
            packets.Add(WorldPackets.CreateDragonSpawn(DragonId, position, IsMounted));
            if (IsMounted)
            {
                packets.Add(WorldPackets.CreateDragonAttributes(DragonId));
                packets.Add(WorldPackets.CreateDragonLink(character.Id, DragonId));
                packets.Add(WorldPackets.CreateLearnedSkills(DragonId, WorldPackets.DragonSkills));
            }
            packets.Add(WorldPackets.RefreshDragonItem(item));
        }
        return packets.ToArray();
    }
}
