using System.Buffers.Binary;
using DragonKnightServer.Database;
using DragonKnightServer.Protocol;

namespace DragonKnightServer;

internal sealed partial class WorldSession
{
    private int silver = character.Silver;

    private static bool IsQuestReady(WorldQuest quest, QuestProgress progress) =>
        quest.KillCount == 0 ? progress.Kills > 0 : progress.Kills >= quest.KillCount;

    private bool MarkTalkQuest(int rawNpcId)
    {
        bool changed = false;
        foreach (var quest in WorldQuests.ForFaction(faction))
        {
            if (quest.KillCount != 0 || quest.Finisher != rawNpcId
                || !quests.TryGetValue(quest.Id, out var progress)
                || progress.Completed || progress.Kills != 0)
                continue;

            if (CharacterRepository.TryMarkQuestTalked(character.Id, quest.Id))
            {
                quests[quest.Id] = progress with { Kills = 1 };
                changed = true;
            }
        }
        return changed;
    }

    internal byte[][] QuestStatePackets()
    {
        var packets = new List<byte[]>();
        foreach (var quest in WorldQuests.ForFaction(faction))
        {
            if (!IsQuestUnlocked(quest)
                || quests.TryGetValue(quest.Id, out var saved) && saved.Completed)
                continue;

            int giverId = NpcPopulation.IdOffset + quest.Giver;
            bool accepted = quests.TryGetValue(quest.Id, out var progress);
            bool ready = accepted && IsQuestReady(quest, progress);
            if (!accepted)
                packets.Add(WorldPackets.CreateQuestMarker(giverId, 1));
            else if (quest.Finisher == quest.Giver)
                packets.Add(WorldPackets.CreateQuestMarker(giverId, ready ? (byte)3 : (byte)2));
            else
            {
                packets.Add(WorldPackets.CreateQuestMarker(giverId, 0));
                packets.Add(WorldPackets.CreateQuestMarker(NpcPopulation.IdOffset + quest.Finisher,
                    ready ? (byte)3 : (byte)2));
            }
        }
        return [.. packets];
    }

    internal byte[][] QuestJournalPackets()
    {
        var active = WorldQuests.ForFaction(faction)
            .Where(quest => quests.TryGetValue(quest.Id, out var progress) && !progress.Completed)
            .Select(quest => (quest, quests[quest.Id]))
            .ToArray();
        var packets = new List<byte[]> { WorldPackets.CreateQuestList(active) };
        foreach (var (quest, progress) in active)
        {
            packets.Add(WorldPackets.CreateQuestDetails(quest, progress, 1));
            packets.Add(WorldPackets.CreateQuestProgress(quest, progress));
            packets.Add(WorldPackets.CreateQuestJournal(quest, progress));
        }
        return [.. packets];
    }

    internal byte[][] QuestMarkerPackets() => QuestStatePackets();

    internal byte[][] HandleNpcPacket(ReadOnlySpan<byte> packet)
    {
        int npcId = BinaryPrimitives.ReadInt32BigEndian(packet[8..]);
        int action = BinaryPrimitives.ReadInt32BigEndian(packet[12..]);
        int selection = packet.Length >= 20 ? BinaryPrimitives.ReadInt32BigEndian(packet[16..]) : action;
        var npc = FindNpc(npcId);
        // Some client builds prepend the player actor id to this request.
        // In that layout the NPC id and dialog option are shifted by four
        // bytes. Accept both layouts so the interaction does not silently
        // disappear when the client and server use different builds.
        if (npc is null && packet.Length >= 20)
        {
            npcId = BinaryPrimitives.ReadInt32BigEndian(packet[12..]);
            action = BinaryPrimitives.ReadInt32BigEndian(packet[16..]);
            selection = action;
            npc = FindNpc(npcId);
        }
        if (npc is not null)
            npcId = npc.Id;
        if (npc is null)
        {
            Console.WriteLine($"Взаимодействие с неизвестным НПС {npcId}, пункт {selection}, карта {character.Map}.");
            return [];
        }

        if (npc.IsDragonEnvoy)
        {
            if (action == 0)
                return [WorldPackets.CreateNpcDialog(npcId,
                    "Приветствую, Харпия. Я Посланник Драконов, драконьи услуги. Чем могу быть полезен?",
                    [
                        ((ushort)0, "Изучить навыки дракона"),
                        ((ushort)0, "Улучшить навыки дракона"),
                        ((ushort)0, "Эволюционировать дракона"),
                        ((ushort)0, "Показать товары"),
                        ((ushort)0, "Воскресить дракона"),
                        ((ushort)0, "Ничего, прощай")
                    ])];

            if (selection == 3)
                return [WorldPackets.CreateNpcShop(npcId, npc.InfoId)];
            if (selection == 2)
                return DragonEvolutionWindowPackets();
            return [WorldPackets.CloseNpcDialog()];
        }

        int rawId = npc.Id - NpcPopulation.IdOffset;
        bool talkedQuest = MarkTalkQuest(rawId);
        var questsAtNpc = WorldQuests.ForFaction(faction)
            .Where(item =>
            {
                if (!IsQuestUnlocked(item)
                    || quests.TryGetValue(item.Id, out var saved) && saved.Completed)
                    return false;

                bool accepted = quests.TryGetValue(item.Id, out var progress);
                bool ready = accepted && IsQuestReady(item, progress);
                return !accepted && item.Giver == rawId
                    || accepted && item.Finisher == rawId && (ready || item.Finisher == item.Giver
                        || item.KillCount == 0);
            })
            .ToArray();

        if (action == 0)
        {
            var options = questsAtNpc
                .Select(item => ((ushort)1, item.Title))
                .ToArray();
            var packets = new List<byte[]> { WorldPackets.CreateNpcDialog(npcId, npc.Name, options) };
            if (talkedQuest)
                packets.AddRange(QuestJournalPackets());
            packets.Add(WorldPackets.CreateQuestMarker(npcId, QuestMarkerState(rawId)));
            if (talkedQuest)
                packets.AddRange(QuestStatePackets());
            return [.. packets];
        }

        if (selection < 0 || selection >= questsAtNpc.Length)
            return [];

        var quest = questsAtNpc[selection];
        var progress = quests.GetValueOrDefault(quest.Id);
        bool acceptedQuest = quests.ContainsKey(quest.Id);
        bool readyQuest = acceptedQuest && IsQuestReady(quest, progress);
        if (!acceptedQuest && quest.Giver == rawId)
            return [WorldPackets.CreateQuestOffer(quest, progress, completed: false),
                WorldPackets.CreateQuestDetails(quest, progress, 0)];

        if (readyQuest && quest.Finisher == rawId)
            return [WorldPackets.CreateQuestOffer(quest, progress, completed: true),
                WorldPackets.CreateQuestDetails(quest, progress, 2)];

        return [WorldPackets.CreateQuestDetails(quest, progress, 1)];
    }

    private byte[][] DragonEvolutionWindowPackets()
    {
        var dragon = inventory.Values.FirstOrDefault(item => item.DragonGrowth >= 100
            && CharacterRepository.DragonEvolutionItems.ContainsKey(item.ItemId));
        if (dragon is null)
        {
            Console.WriteLine("Эволюция: нет детёныша с ростом 100 в инвентаре.");
            return [WorldPackets.CloseNpcDialog()];
        }

        int index = dragon.BackpackIndex;
        return [WorldPackets.CloseNpcDialog(),
            WorldPackets.CreateDragonSlotResult(
                (byte)(WorldPackets.FirstBackpackPage + index / WorldPackets.BackpackPageSize),
                (ushort)(index % WorldPackets.BackpackPageSize), dragon.DragonSlot ?? 0,
                remove: true, itemType: WorldPackets.GrowingDragonType),
            WorldPackets.CreateDragonSlots(character.Id, []),
            WorldPackets.CreateDragonEvolutionWindow(character.Id, dragon)];
    }

    private byte QuestMarkerState(int rawNpcId)
    {
        bool available = false;
        bool pending = false;
        bool ready = false;
        foreach (var quest in WorldQuests.ForFaction(faction))
        {
            if (!IsQuestUnlocked(quest)
                || quests.TryGetValue(quest.Id, out var saved) && saved.Completed)
                continue;

            bool accepted = quests.TryGetValue(quest.Id, out var progress);
            if (!accepted && quest.Giver == rawNpcId)
                available = true;
            else if (accepted && quest.Finisher == rawNpcId)
            {
                bool complete = IsQuestReady(quest, progress);
                if (complete)
                    ready = true;
                else if (quest.Finisher == quest.Giver)
                    pending = true;
            }
        }

        return ready ? (byte)3 : available ? (byte)1 : pending ? (byte)2 : (byte)0;
    }

    private WorldNpc? FindNpc(int clientNpcId) => npcs.FirstOrDefault(item =>
        item.Id == clientNpcId
        || item.Id == clientNpcId - 100_000
        || item.Id == clientNpcId + 100_000
        || item.Id == clientNpcId % NpcPopulation.IdOffset
        || item.Id == NpcPopulation.IdOffset + clientNpcId);

    internal byte[][] HandleQuestPacket(Opcode opcode, ReadOnlySpan<byte> packet)
    {
        if (opcode == Opcode.RequestQuestJournal)
            return QuestJournalPackets();

        int questId = BinaryPrimitives.ReadInt32BigEndian(packet[8..]);
        var quest = WorldQuests.ForFaction(faction).FirstOrDefault(item => item.Id == questId);
        if (quest is null || !IsQuestUnlocked(quest))
            return [];

        var progress = quests.GetValueOrDefault(quest.Id);
        if (opcode is Opcode.RequestQuestDetails or Opcode.RequestQuestTracking)
        {
            int state = progress.Completed ? 2 : quests.ContainsKey(quest.Id) ? 1 : 0;
            return [WorldPackets.CreateQuestDetails(quest, progress, state)];
        }
        if (opcode == Opcode.AcceptQuest)
        {
            if (progress.Completed || quests.ContainsKey(quest.Id))
                return [];
            if (!CharacterRepository.TryAcceptQuest(character.Id, quest.Id))
                return [];
            quests[quest.Id] = new QuestProgress(0, false);
            return [.. QuestJournalPackets(), .. QuestStatePackets(), WorldPackets.CloseNpcDialog()];
        }
        if (opcode == Opcode.CompleteQuest)
        {
            if (!quests.ContainsKey(quest.Id) || progress.Completed || !IsQuestReady(quest, progress))
                return [];
            ushort rewardSelection = packet.Length >= 14
                ? BinaryPrimitives.ReadUInt16BigEndian(packet[12..])
                : (ushort)0;
            var rewardItems = quest.RewardItems;
            if (rewardSelection >= rewardItems.Count)
                return [];
            var completion = CharacterRepository.TryCompleteQuest(character.Id, quest,
                WorldPackets.BackpackPageSize * WorldPackets.BackpackPageCount,
                rewardItems[rewardSelection]);
            if (completion is not { } result)
                return [];
            silver = result.Silver;
            if (result.Item is { } rewardItem)
                inventory[rewardItem.Slot] = rewardItem;
            var experiencePackets = GainExperience(quest.EffectiveExperience);
            quests[quest.Id] = progress with { Completed = true };
            var clearedMarkers = new List<byte[]>
            {
                WorldPackets.CreateQuestMarker(NpcPopulation.IdOffset + quest.Giver, 0)
            };
            if (quest.Finisher != quest.Giver)
                clearedMarkers.Add(WorldPackets.CreateQuestMarker(NpcPopulation.IdOffset + quest.Finisher, 0));
            return [WorldPackets.CreateBackpackUpdate(character.Id, silver,
                    result.Item is { } item ? [(item.BackpackIndex, (CharacterItem?)item)] : []),
                .. experiencePackets,
                .. QuestJournalPackets(),
                .. clearedMarkers,
                .. QuestStatePackets(),
                WorldPackets.CloseNpcDialog()];
        }
        if (opcode == Opcode.AbandonQuest && CharacterRepository.TryAbandonQuest(character.Id, quest.Id))
        {
            quests.Remove(quest.Id);
            var clearedMarkers = new List<byte[]>
            {
                WorldPackets.CreateQuestMarker(NpcPopulation.IdOffset + quest.Giver, 0)
            };
            if (quest.Finisher != quest.Giver)
                clearedMarkers.Add(WorldPackets.CreateQuestMarker(NpcPopulation.IdOffset + quest.Finisher, 0));
            return [.. clearedMarkers,
                .. QuestJournalPackets(), .. QuestStatePackets()];
        }
        return [];
    }

    internal byte[][] BuyFromDragonEnvoy(ReadOnlySpan<byte> packet)
    {
        int npcId = BinaryPrimitives.ReadInt32BigEndian(packet[8..]);
        var npc = FindNpc(npcId);
        if (npc?.IsDragonEnvoy != true)
            return [];
        npcId = npc.Id;
        int infoId = BinaryPrimitives.ReadInt32BigEndian(packet[12..]);
        if (infoId != npc.InfoId)
            return [];
        ushort itemIndex = BinaryPrimitives.ReadUInt16BigEndian(packet[16..]);
        ushort quantity = packet[18] is 0 ? (ushort)1 : packet[18];
        if (itemIndex >= WorldQuests.DragonEnvoyStock.Count)
            return [WorldPackets.CreateShopPurchaseResult(false, 0, 0)];
        var stock = WorldQuests.DragonEnvoyStock[itemIndex];
        var result = CharacterRepository.TryBuy(character.Id, stock, quantity);
        if (result is not { } purchase)
            return [WorldPackets.CreateShopPurchaseResult(false, stock.ItemId, 0)];
        silver = purchase.Silver;
        foreach (var item in purchase.Items)
            inventory[item.Slot] = item;
        var updates = purchase.Items.Select(item => (item.BackpackIndex, (CharacterItem?)item)).ToArray();
        return [WorldPackets.CreateShopPurchaseResult(true, stock.ItemId, stock.Price * quantity),
            WorldPackets.CreateBackpackUpdate(character.Id, silver, updates)];
    }

    internal bool IsQuestUnlocked(WorldQuest quest)
    {
        if (quest.Level > level)
            return false;
        if (quest.SideQuest)
            return true;
        var list = WorldQuests.ForFaction(faction);
        int index = Array.IndexOf(list.ToArray(), quest);
        return index <= 0 || quests.TryGetValue(list[index - 1].Id, out var previous) && previous.Completed;
    }

    private WorldQuest? CurrentQuest() => WorldQuests.ForFaction(faction)
        .FirstOrDefault(quest => IsQuestUnlocked(quest) && !quests.GetValueOrDefault(quest.Id).Completed);

    internal void AddQuestKill(ushort monsterTemplate)
    {
        foreach (var quest in WorldQuests.ForFaction(faction))
        {
            if (!quests.TryGetValue(quest.Id, out var progress) || progress.Completed || quest.Monster != monsterTemplate
                || progress.Kills >= quest.KillCount)
                continue;
            if (CharacterRepository.TryCountQuestKill(character.Id, quest.Id, progress.Kills))
                quests[quest.Id] = progress with { Kills = progress.Kills + 1 };
        }
    }
}
