using System.Buffers.Binary;
using DragonKnightServer.Database;
using DragonKnightServer.Protocol;

namespace DragonKnightServer;

internal sealed partial class WorldSession
{
    private int silver = character.Silver;

    internal byte[][] QuestStatePackets()
    {
        var quest = CurrentQuest();
        if (quest is null)
            return [];
        var packets = new List<byte[]> { WorldPackets.CreateQuestMarker(NpcPopulation.IdOffset + quest.Giver, true) };
        if (quests.TryGetValue(quest.Id, out var progress) && !progress.Completed
            && quest.Finisher != quest.Giver)
            packets.Add(WorldPackets.CreateQuestMarker(NpcPopulation.IdOffset + quest.Finisher,
                progress.Kills >= quest.KillCount || quest.KillCount == 0));
        return [.. packets];
    }

    internal byte[][] QuestMarkerPackets() => QuestStatePackets();

    internal byte[][] HandleNpcPacket(ReadOnlySpan<byte> packet)
    {
        int npcId = BinaryPrimitives.ReadInt32BigEndian(packet[8..]);
        int option = BinaryPrimitives.ReadInt32BigEndian(packet[12..]);
        var npc = FindNpc(npcId);
        // Some client builds prepend the player actor id to this request.
        // In that layout the NPC id and dialog option are shifted by four
        // bytes. Accept both layouts so the interaction does not silently
        // disappear when the client and server use different builds.
        if (npc is null && packet.Length >= 20)
        {
            npcId = BinaryPrimitives.ReadInt32BigEndian(packet[12..]);
            option = BinaryPrimitives.ReadInt32BigEndian(packet[16..]);
            npc = FindNpc(npcId);
        }
        if (npc is not null)
            npcId = npc.Id;
        if (npc is null)
        {
            Console.WriteLine($"Взаимодействие с неизвестным НПС {npcId}, пункт {option}, карта {character.Map}.");
            return [];
        }

        if (npc.IsDragonEnvoy)
            return option == 0
                ? [WorldPackets.CreateNpcDialog(npcId,
                    "Приветствую, Харпия. Я Посланник Драконов, драконьи услуги. Чем могу быть полезен?",
                    [
                        ((ushort)0, "Изучить навыки дракона"),
                        ((ushort)0, "Улучшить навыки дракона"),
                        ((ushort)0, "Эволюционировать дракона"),
                        ((ushort)0, "Показать товары"),
                        ((ushort)0, "Воскресить дракона"),
                        ((ushort)0, "Ничего, прощай")
                    ])]
                : [WorldPackets.CreateNpcShop(npcId, npc.InfoId)];

        int rawId = npc.Id - NpcPopulation.IdOffset;
        var quest = WorldQuests.ForFaction(faction).FirstOrDefault(item =>
            IsQuestUnlocked(item) && !quests.GetValueOrDefault(item.Id).Completed
            && (item.Giver == rawId || item.Finisher == rawId));
        if (quest is null || !IsQuestUnlocked(quest))
            return option == 0 ? [WorldPackets.CreateNpcDialog(npcId, npc.Name, [])] : [];

        var progress = quests.GetValueOrDefault(quest.Id);
        bool accepted = quests.ContainsKey(quest.Id);
        bool ready = accepted && (quest.KillCount == 0 || progress.Kills >= quest.KillCount);
        var options = progress.Completed
            ? Array.Empty<(ushort, string)>()
            : [((ushort)1, quest.Title)];
        if (option == 0)
            return [WorldPackets.CreateNpcDialog(npcId, npc.Name, options),
                WorldPackets.CreateQuestMarker(npcId, !accepted)];

        if (!accepted && quest.Giver == rawId)
            return [WorldPackets.CreateQuestOffer(quest, progress, completed: false),
                WorldPackets.CreateQuestDetails(quest, progress, 0)];

        if (ready && quest.Finisher == rawId)
            return [WorldPackets.CreateQuestOffer(quest, progress, completed: true),
                WorldPackets.CreateQuestDetails(quest, progress, 2)];

        return [WorldPackets.CreateQuestDetails(quest, progress, 0)];
    }

    private WorldNpc? FindNpc(int clientNpcId) => npcs.FirstOrDefault(item =>
        item.Id == clientNpcId
        || item.Id == clientNpcId - 100_000
        || item.Id == clientNpcId + 100_000
        || item.Id == clientNpcId % NpcPopulation.IdOffset
        || item.Id == NpcPopulation.IdOffset + clientNpcId);

    internal byte[][] HandleQuestPacket(Opcode opcode, ReadOnlySpan<byte> packet)
    {
        int questId = BinaryPrimitives.ReadInt32BigEndian(packet[8..]);
        var quest = WorldQuests.ForFaction(faction).FirstOrDefault(item => item.Id == questId);
        if (quest is null || !IsQuestUnlocked(quest))
            return [];

        var progress = quests.GetValueOrDefault(quest.Id);
        if (opcode is Opcode.RequestQuestDetails or Opcode.RequestQuestJournal)
            return [WorldPackets.CreateQuestDetails(quest, progress, progress.Completed ? 2 : 0)];
        if (opcode == Opcode.AcceptQuest)
        {
            if (progress.Completed || quests.ContainsKey(quest.Id))
                return [];
            if (!CharacterRepository.TryAcceptQuest(character.Id, quest.Id))
                return [];
            quests[quest.Id] = new QuestProgress(0, false);
            return [WorldPackets.CreateQuestDetails(quest, quests[quest.Id], 1), WorldPackets.CreateQuestProgress(quest, quests[quest.Id])];
        }
        if (opcode == Opcode.CompleteQuest)
        {
            if (!quests.ContainsKey(quest.Id) || progress.Kills < quest.KillCount)
                return [];
            var newSilver = CharacterRepository.TryCompleteQuest(character.Id, quest);
            if (newSilver is not { } balance)
                return [];
            silver = balance;
            quests[quest.Id] = progress with { Completed = true };
            return [WorldPackets.CreateQuestOffer(quest, quests[quest.Id], completed: true), WorldPackets.CreateBackpackUpdate(character.Id, silver)];
        }
        if (opcode == Opcode.AbandonQuest && CharacterRepository.TryAbandonQuest(character.Id, quest.Id))
        {
            quests.Remove(quest.Id);
            return [WorldPackets.CreateQuestMarker(NpcPopulation.IdOffset + quest.Giver, true)];
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
        if (quest.Level > character.Level)
            return false;
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
