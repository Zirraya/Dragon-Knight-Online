using System.Buffers.Binary;
using DragonKnightServer.Database;
using DragonKnightServer.Protocol;

namespace DragonKnightServer;

internal sealed partial class WorldSession(WorldCharacter character)
{
    private const int SpawnDistance = 12_000;
    private const int DespawnDistance = 15_000;
    private (int X, int Y, int Z) position = WorldPackets.StartingPosition(character);
    private readonly Dictionary<int, CreatureSpawn> visibleCreatures = [];
    private readonly Dictionary<int, int> injuredCreatures = [];
    private readonly Dictionary<int, (CreatureSpawn Spawn, long CorpseUntil, long RespawnAt)> defeatedCreatures = [];
    private readonly IReadOnlyList<WorldNpc> npcs = NpcPopulation.ForMap(character.Map);
    private readonly HashSet<int> visibleNpcs = [];
    private readonly Dictionary<ushort, CharacterItem> inventory = character.Inventory.ToDictionary(item => item.Slot);
    private int level = character.Level;
    private int experience = character.Experience;
    private int hitPoints = character.HitPoints;
    private int mana = character.Mana;
    private byte[]? quickbar = character.Quickbar;
    private CharacterItem? companion;
    private ushort? pendingBattleDragonSlot;
    private ushort? pendingFeedItemSlot;
    private bool positionChanged;
    private bool moving;
    private readonly Dictionary<int, QuestProgress> quests = character.Quests.ToDictionary(pair => pair.Key, pair => pair.Value);
    private readonly ushort faction = CharacterOrigins.ReadFaction(character.Appearance ?? []);
    private int DragonId => (IsMounted ? 0x007A0000 : 0x007B0000) + character.Id;
    private bool IsMounted => companion?.ItemId == CharacterRepository.ElvenDragonItem
        || companion is { } item && CharacterRepository.DragonContractItems.Contains(item.ItemId);
    internal Task? BattleDragonCastDelay { get; private set; }

    private WorldCharacter CurrentCharacter => character with
    {
        Level = level,
        Experience = experience,
        HitPoints = hitPoints,
        Mana = mana
    };

    private byte[][] GainExperience(int amount)
    {
        var result = CharacterRepository.TryAddExperience(character.Id, character.Template, amount);
        if (result is not { } gain)
            return [];

        level = gain.Level;
        experience = gain.Experience;
        hitPoints = gain.HitPoints;
        mana = gain.Mana;
        return [WorldPackets.CreatePlayerProgress(CurrentCharacter)];
    }

    internal static ushort[] GetPlayerSkills(int template) => template switch
    {
        1 or 6 => [1, 3061, 9981, 20241, 5701, 1301, 1341, 1381, 1321, 2261, 1401,
            1421, 1581, 1561, 1361, 1441, 1501, 1481, 1621, 1521, 20081, 1681, 2281,
            20221, 1661, 1701, 3921, 3901, 3881, 3941, 3961, 3981],
        2 or 7 => [1, 3061, 9981, 20261, 5721, 1921, 1741, 2301, 1761, 1721, 1821,
            1881, 20101, 1781, 2021, 1841, 2321, 1981, 2061, 1961, 20121, 2041, 2081,
            2001, 1901, 2101, 3921, 3901, 3881, 3941, 3961, 3981],
        3 or 8 => [1, 3021, 9981, 20281, 21321, 881, 901, 1041, 1221, 921, 1201, 20041,
            2161, 1001, 2201, 20061, 1261, 1141, 1241, 1021, 2221, 2181, 1161, 20141,
            2241, 1281, 3921, 3901, 3881, 3941, 3961, 3981],
        4 or 9 => [1, 3081, 9981, 20321, 21301, 21, 141, 2121, 20001, 121, 61, 20161,
            401, 201, 281, 301, 241, 261, 641, 381, 161, 321, 361, 181, 421, 441,
            3921, 3901, 3881, 3941, 3961, 3981],
        5 or 10 => [1, 3081, 9981, 20301, 21301, 461, 481, 581, 781, 601, 661, 20021,
            541, 681, 221, 721, 821, 621, 20181, 761, 701, 2141, 801, 20201, 841, 861,
            3921, 3901, 3881, 3941, 3961, 3981],
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
        foreach (var npc in npcs)
        {
            bool visible = visibleNpcs.Contains(npc.Id);
            bool nearby = WithinDistance(npc.X, npc.Y, visible ? DespawnDistance : SpawnDistance);
            if (nearby == visible)
                continue;

            if (nearby)
            {
                visibleNpcs.Add(npc.Id);
                packets.Add(WorldPackets.CreateNpcSpawn(npc));
            }
            else
            {
                visibleNpcs.Remove(npc.Id);
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

    internal byte[][] RefreshDragonSatiety()
    {
        var refreshed = CharacterRepository.RefreshInventory(character.Id);
        var changed = false;
        foreach (var item in refreshed)
        {
            if (!inventory.TryGetValue(item.Slot, out var current))
                continue;
            if (current.DragonSatiety != item.DragonSatiety || current.DragonSatietyUpdated != item.DragonSatietyUpdated)
                changed = true;
            inventory[item.Slot] = item;
        }
        return changed
            ? [WorldPackets.CreateInventory(character.Id, silver, inventory.Values, Environment.TickCount), GetDragonSlots()]
            : [];
    }

    internal byte[][] MoveBackpackItem(ReadOnlySpan<byte> packet)
    {
        ushort? firstIndex = ReadBackpackIndex(packet[8], BinaryPrimitives.ReadUInt16BigEndian(packet[9..]));
        ushort? secondIndex = ReadBackpackIndex(packet[11], BinaryPrimitives.ReadUInt16BigEndian(packet[12..]));
        ushort quantity = BinaryPrimitives.ReadUInt16BigEndian(packet[14..]);
        if (firstIndex is not ushort first || secondIndex is not ushort second || first == second)
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
        if (source is null || quantity > source.Quantity)
            return [];

        if (quantity > 0 && quantity < source.Quantity)
        {
            if (target is not null || CharacterRepository.TrySplitBackpackStack(character.Id, source, to, quantity) is not { } split)
                return [];

            var remainder = source with { Quantity = (ushort)(source.Quantity - quantity) };
            inventory[remainder.Slot] = remainder;
            inventory[split.Slot] = split;
            return [WorldPackets.CreateBackpackUpdate(character.Id, silver, (from, remainder), (to, split))];
        }

        if (!CharacterRepository.TryMoveBackpackItem(character.Id, source, to, target))
            return [];

        var moved = source with { BackpackSlot = to };
        var swapped = target is null ? null : target with { BackpackSlot = from };
        inventory[moved.Slot] = moved;
        if (swapped is not null)
            inventory[swapped.Slot] = swapped;
        if (companion is not null)
            companion = inventory[companion.Slot];

        return [WorldPackets.CreateBackpackUpdate(character.Id, silver, (from, swapped), (to, moved)), GetDragonSlots()];
    }

    internal byte[][] ToggleEquipment(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 13)
            return [];

        byte? equipmentSlot = FindEquipmentSlotTag(packet);
        if (equipmentSlot is not byte slot)
            return [];

        var result = CharacterRepository.TryToggleEquipment(character.Id, character.Template, slot,
            WorldPackets.BackpackPageSize * WorldPackets.BackpackPageCount);
        if (result is null)
        {
            result = ToggleEquipmentFromSession(slot);
            if (result is null || !CharacterRepository.TrySaveEquipmentState(character.Id, result.Item))
            {
                Console.WriteLine($"Экипировка персонажа {character.Id}: слот {slot} не найден или рюкзак заполнен.");
                return [];
            }
        }

        inventory[result.Item.Slot] = result.Item;
        var snapshot = character with { Inventory = inventory.Values.ToArray() };
        var equipment = WorldPackets.EquippedItems(snapshot);
        int tick = Environment.TickCount;
        return
        [
            WorldPackets.CreateInventory(character.Id, silver, inventory.Values, unchecked(tick + 1)),
            GetDragonSlots(),
            WorldPackets.CreateEquipmentDetails(character.Id, equipment, tick),
            WorldPackets.CreateEquipmentList(snapshot, equipment, tick),
            WorldPackets.CreateEquipmentItemList(character.Id, equipment)
        ];
    }

    private static byte? FindEquipmentSlotTag(ReadOnlySpan<byte> packet)
    {
        for (int offset = LoginPackets.HeaderSize; offset + sizeof(ushort) <= packet.Length; offset++)
        {
            ushort bigEndian = BinaryPrimitives.ReadUInt16BigEndian(packet[offset..]);
            if (bigEndian is >= 0x4001 and <= 0x400F)
                return (byte)(bigEndian - 0x4000);

            ushort littleEndian = BinaryPrimitives.ReadUInt16LittleEndian(packet[offset..]);
            if (littleEndian is >= 0x4001 and <= 0x400F)
                return (byte)(littleEndian - 0x4000);
        }

        return null;
    }

    private EquipmentToggleResult? ToggleEquipmentFromSession(byte equipmentSlot)
    {
        var item = inventory.Values.FirstOrDefault(candidate =>
            candidate.EquipmentSlot == equipmentSlot && candidate.IsEquipped)
            ?? inventory.Values.FirstOrDefault(candidate =>
                candidate.EquipmentSlot == equipmentSlot && candidate.BackpackSlot.HasValue);

        if (item is null)
        {
            var definition = StarterEquipment.ForTemplate(character.Template)
                .FirstOrDefault(candidate => candidate.EquipmentSlot == equipmentSlot);
            if (definition is null)
                return null;

            ushort instanceSlot = (ushort)Enumerable.Range(2, ushort.MaxValue - 1)
                .First(index => !inventory.ContainsKey((ushort)index));
            item = definition with { Slot = instanceSlot };
            inventory[item.Slot] = item;
        }

        if (item.IsEquipped)
        {
            var occupied = inventory.Values.Where(candidate => candidate.BackpackIndex <
                    WorldPackets.BackpackPageSize * WorldPackets.BackpackPageCount)
                .Select(candidate => candidate.BackpackIndex).ToHashSet();
            int destination = Enumerable.Range(0, WorldPackets.BackpackPageSize * WorldPackets.BackpackPageCount)
                .FirstOrDefault(index => !occupied.Contains((ushort)index), -1);
            if (destination < 0)
                return null;

            return new EquipmentToggleResult(item with { BackpackSlot = (ushort)destination }, false);
        }

        return item.BackpackSlot.HasValue
            ? new EquipmentToggleResult(item with { BackpackSlot = null }, true)
            : null;
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
        if (ReadBackpackIndex(bag, slot) is ushort feedIndex
            && FindBackpackItem(feedIndex) is { } feed
            && CharacterRepository.DragonFeedItems.Contains(feed.ItemId))
        {
            if (action > 1)
                return [];
            pendingFeedItemSlot = remove ? null : feed.Slot;
            return [WorldPackets.CreateDragonSlotResult(bag, slot, destination, remove,
                itemType: WorldPackets.GrowingDragonType)];
        }
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

    internal byte[][] FeedDragon(ReadOnlySpan<byte> packet)
    {
        var egg = inventory.Values.FirstOrDefault(item => item.DragonSlot is byte
            && CharacterRepository.IsGrowingDragon(item.ItemId));
        egg ??= companion;
        egg ??= inventory.Values.FirstOrDefault(item =>
            CharacterRepository.DragonContractItems.Contains(item.ItemId));
        var food = pendingFeedItemSlot is ushort foodSlot
            ? inventory.GetValueOrDefault(foodSlot)
            : inventory.Values.FirstOrDefault(item => CharacterRepository.DragonFeedItems.Contains(item.ItemId)
                && item.BackpackIndex < WorldPackets.BackpackPageSize * WorldPackets.BackpackPageCount);
        if (egg is null || food is null)
            return [];

        byte growthGain = CharacterRepository.DragonFeedGrowth.GetValueOrDefault(food.ItemId);
        byte satietyGain = CharacterRepository.DragonFeedSatiety.GetValueOrDefault(food.ItemId);
        if (growthGain == 0 && satietyGain == 0)
            return [];
        var fed = CharacterRepository.TryFeedDragon(character.Id, egg, food, growthGain, satietyGain);
        if (fed is null)
            return [];

        var refreshed = CharacterRepository.RefreshInventory(character.Id);
        inventory.Clear();
        foreach (var item in refreshed)
            inventory[item.Slot] = item;
        if (companion is { } active && inventory.TryGetValue(active.Slot, out var refreshedCompanion))
            companion = refreshedCompanion;
        pendingFeedItemSlot = food.Quantity > 1 ? food.Slot : null;
        return [WorldPackets.CreateInventory(character.Id, silver, inventory.Values, Environment.TickCount),
            GetDragonSlots()];
    }

    internal byte[][] EvolveDragon(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 2)
            return [];

        ushort selection = BinaryPrimitives.ReadUInt16BigEndian(packet[^2..]);
        if (CharacterRepository.HatchlingDragonItems.Contains(selection))
        {
            var egg = inventory.Values.FirstOrDefault(item => item.ItemId == CharacterRepository.StarterDragonEgg
                && item.DragonSlot is byte && item.DragonGrowth >= 100);
            if (egg is null)
                return [];
            var hatched = CharacterRepository.TryHatchDragon(character.Id, egg, selection);
            if (hatched is null)
                return [];
            inventory[hatched.Slot] = hatched;
        }
        else
        {
            var dragon = inventory.Values.FirstOrDefault(item => item.DragonGrowth >= 100
                && CharacterRepository.DragonEvolutionItems.ContainsKey(item.ItemId));
            if (dragon is null)
                return [];
            var evolved = CharacterRepository.TryEvolveDragon(character.Id, dragon);
            if (evolved is null)
                return [];
            inventory[evolved.Slot] = evolved;
            return [WorldPackets.CreateDragonEvolutionResult(character.Id, dragon),
                WorldPackets.CreateInventory(character.Id, silver, inventory.Values, Environment.TickCount),
                GetDragonSlots()];
        }
        return [WorldPackets.CreateInventory(character.Id, silver, inventory.Values, Environment.TickCount),
            GetDragonSlots()];
    }

    internal byte[][] Call(ReadOnlySpan<byte> packet)
    {
        byte bag = packet[8];
        ushort slot = BinaryPrimitives.ReadUInt16BigEndian(packet[9..]);
        byte action = packet[11];
        Console.WriteLine($"CallDragon: bag={bag}, slot={slot}, action={action}, size={packet.Length}");
        // При вызове с земли передаётся один актёр, а при использовании верхом клиент может дописать второй ID.
        if (action > 8 || !IsDragonActionActor(BinaryPrimitives.ReadInt32BigEndian(packet[12..]))
            || (packet.Length == 20 && !IsDragonActionActor(BinaryPrimitives.ReadInt32BigEndian(packet[16..]))))
            return [];

        var item = bag == 0 ? FindAssignedDragon(slot) : FindDragonItem(bag, slot);
        if (item is null && action <= 1)
            item = inventory.Values.FirstOrDefault(candidate => candidate.ItemId == CharacterRepository.StarterDragonEgg
                && candidate.DragonSlot is byte)
                ?? inventory.Values.FirstOrDefault(candidate =>
                    CharacterRepository.HatchlingDragonItems.Contains(candidate.ItemId)
                    || CharacterRepository.DragonContractItems.Contains(candidate.ItemId));
        if (action >= 2)
        {
            var egg = item?.ItemId == CharacterRepository.StarterDragonEgg
                ? item : inventory.Values.FirstOrDefault(candidate => candidate.ItemId == CharacterRepository.StarterDragonEgg
                    && candidate.DragonSlot is byte);
            var hatched = egg is null ? null : CharacterRepository.TryHatchDragon(character.Id, egg,
                CharacterRepository.HatchlingDragonItems[action - 2]);
            if (hatched is null)
                return [];
            inventory[hatched.Slot] = hatched;
            if (companion?.Slot == hatched.Slot)
                companion = null;
            return [WorldPackets.CreateInventory(character.Id, silver, inventory.Values, Environment.TickCount), GetDragonSlots()];
        }
        if (item is null)
            return [];
        if (action is 0 or 1 && item.ItemId == CharacterRepository.StarterDragonEgg
            && item.DragonGrowth >= 100)
        {
            int index = item.BackpackIndex;
            return OpenHatchSelection(item);
        }
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
        if (skill is WorldPackets.BattleDragonSummonSkill or 28096 or 28001)
        {
            if (!IsDragonActionActor(actorId))
                return [];
            var item = skill == WorldPackets.BattleDragonSummonSkill
                ? inventory.Values.FirstOrDefault(candidate => candidate.ItemId == CharacterRepository.ElvenDragonItem)
                : skill == 28001
                    ? inventory.Values.FirstOrDefault(candidate => CharacterRepository.DragonContractItems.Contains(candidate.ItemId))
                    : inventory.Values.FirstOrDefault(candidate => candidate.ItemId == CharacterRepository.StarterDragonEgg
                        && candidate.DragonSlot is byte && candidate.DragonGrowth >= 100)
                        ?? inventory.Values.FirstOrDefault(candidate => candidate.ItemId == CharacterRepository.YoungSilverDragonItem
                            || CharacterRepository.HatchlingDragonItems.Contains(candidate.ItemId)
                            || CharacterRepository.DragonContractItems.Contains(candidate.ItemId));
            if (skill == 28096 && item?.ItemId == CharacterRepository.StarterDragonEgg)
                return OpenHatchSelection(item);
            return item is null ? [] : UseDragonItem(item);
        }

        var skills = IsMounted ? WorldPackets.DragonSkills : GetPlayerSkills(character.Template);
        if ((actorId != character.Id && (!IsMounted || actorId != DragonId)) || !skills.Contains(skill))
            return [];

        int target = BinaryPrimitives.ReadInt32BigEndian(packet[11..]);
        if (!visibleCreatures.TryGetValue(target, out var creature) || defeatedCreatures.ContainsKey(target))
            return [];

        const int airAttackDistance = 5_000;
        if (creature.Template.AirborneName is not null && (!IsMounted
            || !WithinDistance(creature.Position.X, creature.Position.Y, airAttackDistance)
            || Math.Abs((long)position.Z - creature.Position.Z) > airAttackDistance))
            return [];

        int remaining = injuredCreatures.GetValueOrDefault(target, creature.Template.HitPoints);
        // Test combat damage is five times the normal base values.
        int damage = Math.Min(remaining, IsMounted ? 1000 : 1000);
        int newHealth = remaining - damage;
        if (newHealth == 0)
        {
            injuredCreatures.Remove(target);
            long corpseUntil = Environment.TickCount64 + creature.Template.CorpseMs;
            defeatedCreatures.Add(target, (creature, corpseUntil, corpseUntil + creature.Template.RefreshMs));
            AddQuestKill(creature.Template.Id);
        }
        else
            injuredCreatures[target] = newHealth;

        var hit = WorldPackets.CreateHit(IsMounted ? DragonId : character.Id, creature, skill, damage, newHealth);
        var experiencePackets = newHealth == 0
            ? GainExperience(Math.Max(1, creature.Template.Level * 5))
            : [];
        var questMarkers = newHealth == 0 ? QuestStatePackets() : [];
        var questJournal = newHealth == 0 ? QuestJournalPackets() : [];
        if (newHealth == 0 && MonsterLoot.Roll(creature.Template.Id, Random.Shared.Next(100)) is { } drop
            && CharacterRepository.TryStoreLoot(character.Id, drop, WorldPackets.BackpackPageSize * WorldPackets.BackpackPageCount)
                is { } storedItem)
        {
            inventory[storedItem.Slot] = storedItem;
            return [.. hit, .. experiencePackets, .. questJournal, .. questMarkers,
                WorldPackets.CreateBackpackUpdate(character.Id, silver, (storedItem.BackpackIndex, storedItem))];
        }
        return [.. hit, .. experiencePackets, .. questJournal, .. questMarkers];
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
        return item is not null && (WorldPackets.DragonSlotType(item.ItemId) != 0
            || CharacterRepository.DragonContractItems.Contains(item.ItemId)) ? item : null;
    }

    private bool IsDragonActionActor(int actorId) =>
        actorId == character.Id || (companion is not null && actorId == DragonId);

    private CharacterItem? FindAssignedDragon(ushort slotTag)
    {
        // Клиент передаёт индекс ячейки выращивания или тег ячейки боевого дракона (50–55).
        if (slotTag < 5)
            return inventory.Values.FirstOrDefault(item => (item.ItemId == CharacterRepository.YoungSilverDragonItem
                || item.ItemId == CharacterRepository.StarterDragonEgg
                || CharacterRepository.HatchlingDragonItems.Contains(item.ItemId))
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
        if (item.ItemId == CharacterRepository.StarterDragonEgg)
            return [];
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

    private byte[][] OpenHatchSelection(CharacterItem egg)
    {
        int index = egg.BackpackIndex;
        return [WorldPackets.CreateDragonSlotResult(
            (byte)(WorldPackets.FirstBackpackPage + index / WorldPackets.BackpackPageSize),
            (ushort)(index % WorldPackets.BackpackPageSize), egg.DragonSlot ?? 0,
            remove: true, itemType: WorldPackets.GrowingDragonType, result: 0),
            WorldPackets.CreateInventory(character.Id, silver, inventory.Values, Environment.TickCount),
            GetDragonSlots(),
            WorldPackets.CreateHatchOptions(character.Id, egg)];
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
            packets.Add(WorldPackets.CreateDragonSpawn(DragonId, position, IsMounted, item.ItemId));
            if (IsMounted)
            {
                packets.Add(WorldPackets.CreateDragonAttributes(DragonId, item.ItemId));
                packets.Add(WorldPackets.CreateDragonLink(character.Id, DragonId));
                packets.Add(WorldPackets.CreateLearnedSkills(DragonId, WorldPackets.DragonSkills));
            }
            packets.Add(WorldPackets.RefreshDragonItem(item));
        }
        return packets.ToArray();
    }
}
