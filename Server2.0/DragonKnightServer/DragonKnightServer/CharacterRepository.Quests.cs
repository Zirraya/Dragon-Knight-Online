using MySqlConnector;
using DragonKnightServer.Protocol;

namespace DragonKnightServer.Database;

public partial class CharacterRepository
{
    internal readonly record struct QuestCompletionResult(int Silver, CharacterItem? Item);
    private static bool questTableReady;

    private static void EnsureQuestTable(MySqlConnection db)
    {
        lock (SchemaLock)
        {
            if (questTableReady)
                return;

            using var cmd = db.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS character_quests
                (
                    character_id INT NOT NULL,
                    quest_id INT NOT NULL,
                    kills SMALLINT UNSIGNED NOT NULL DEFAULT 0,
                    completed BOOLEAN NOT NULL DEFAULT FALSE,
                    PRIMARY KEY (character_id, quest_id),
                    FOREIGN KEY (character_id) REFERENCES characters (id) ON DELETE CASCADE
                )
                """;
            cmd.ExecuteNonQuery();
            questTableReady = true;
        }
    }

    private static void GrantStarterEgg(MySqlCommand cmd, int characterId)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT starter_egg_granted FROM characters WHERE id = @id FOR UPDATE";
        cmd.Parameters.AddWithValue("@id", characterId);
        var granted = cmd.ExecuteScalar();
        if (granted is null || Convert.ToBoolean(granted))
            return;

        var items = ReadInventory(cmd, characterId, forUpdate: true);
        if (items.Any(item => item.ItemId == StarterDragonEgg))
        {
            cmd.Parameters.Clear();
            cmd.CommandText = "UPDATE characters SET starter_egg_granted = TRUE WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", characterId);
            cmd.ExecuteNonQuery();
            return;
        }
        if (FindEmptyBackpackCell(items) is not { } cell)
            return;

        InsertBackpackItem(cmd, characterId, items, StarterDragonEgg, cell, 1);
        cmd.Parameters.Clear();
        cmd.CommandText = "UPDATE characters SET starter_egg_granted = TRUE WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", characterId);
        cmd.ExecuteNonQuery();
    }

    internal static void GrantStarterDragonEssence(MySqlCommand cmd, int characterId)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT starter_essence_granted FROM characters WHERE id = @id FOR UPDATE";
        cmd.Parameters.AddWithValue("@id", characterId);
        if (Convert.ToBoolean(cmd.ExecuteScalar()))
            return;

        var items = ReadInventory(cmd, characterId, forUpdate: true);
        int quantity = items.Where(item => item.ItemId == StarterDragonEssenceItem)
            .Sum(item => item.Quantity);
        int missing = Math.Max(0, StarterDragonEssenceQuantity - quantity);
        if (missing == 0)
        {
            MarkStarterDragonEssenceGranted(cmd, characterId);
            return;
        }

        var stack = items.FirstOrDefault(item => item.ItemId == StarterDragonEssenceItem
            && item.Quantity <= ushort.MaxValue - missing);
        if (stack is not null)
        {
            UpdateItemQuantity(cmd, characterId, stack with { Quantity = (ushort)(stack.Quantity + missing) });
            return;
        }

        if (FindEmptyBackpackCell(items) is not { } cell)
            return;

        InsertBackpackItem(cmd, characterId, items, StarterDragonEssenceItem, cell, (ushort)missing);
        MarkStarterDragonEssenceGranted(cmd, characterId);
    }

    private static void MarkStarterDragonEssenceGranted(MySqlCommand cmd, int characterId)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = "UPDATE characters SET starter_essence_granted = TRUE WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", characterId);
        cmd.ExecuteNonQuery();
    }

    private static ushort? FindEmptyBackpackCell(List<CharacterItem> items)
    {
        int capacity = WorldPackets.BackpackPageSize * WorldPackets.BackpackPageCount;
        var occupied = items.Select(item => item.BackpackIndex).ToHashSet();
        for (ushort cell = 0; cell < capacity; cell++)
        {
            if (!occupied.Contains(cell))
                return cell;
        }
        return null;
    }

    private static IReadOnlyDictionary<int, QuestProgress> ReadQuests(MySqlCommand cmd, int characterId)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT quest_id, kills, completed FROM character_quests WHERE character_id = @id";
        cmd.Parameters.AddWithValue("@id", characterId);
        using var reader = cmd.ExecuteReader();
        var quests = new Dictionary<int, QuestProgress>();
        while (reader.Read())
            quests.Add(reader.GetInt32(0), new QuestProgress(reader.GetInt32(1), reader.GetBoolean(2)));
        return quests;
    }

    internal static bool TryAcceptQuest(int characterId, int questId) =>
        TryChangeQuest(characterId, questId, """
            INSERT INTO character_quests (character_id, quest_id) VALUES (@id, @quest)
            """);

    internal static bool TryAbandonQuest(int characterId, int questId) =>
        TryChangeQuest(characterId, questId, """
            DELETE FROM character_quests WHERE character_id = @id AND quest_id = @quest AND completed = FALSE
            """);

    internal static bool TryCountQuestKill(int characterId, int questId, int previousKills) =>
        TryChangeQuest(characterId, questId, """
            UPDATE character_quests SET kills = kills + 1
            WHERE character_id = @id AND quest_id = @quest AND kills = @kills AND completed = FALSE
            """, previousKills);

    internal static bool TryMarkQuestTalked(int characterId, int questId) =>
        TryChangeQuest(characterId, questId, """
            UPDATE character_quests SET kills = 1
            WHERE character_id = @id AND quest_id = @quest AND kills = 0 AND completed = FALSE
            """);

    private static bool TryChangeQuest(int characterId, int questId, string sql, int? previousKills = null)
    {
        try
        {
            using var db = Database.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@id", characterId);
            cmd.Parameters.AddWithValue("@quest", questId);
            if (previousKills is { } kills)
                cmd.Parameters.AddWithValue("@kills", kills);
            return cmd.ExecuteNonQuery() == 1;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось сохранить задание {questId} персонажа {characterId}. Код MySQL {ex.Number}.");
            return false;
        }
    }

    internal static QuestCompletionResult? TryCompleteQuest(int characterId, WorldQuest quest, int backpackCapacity,
        ushort rewardItem)
    {
        try
        {
            using var db = Database.Open();
            using var transaction = db.BeginTransaction();
            using var cmd = db.CreateCommand();
            cmd.Transaction = transaction;
            if (LockSilver(cmd, characterId) is not { } balance || balance > int.MaxValue - quest.Reward)
                return null;

            cmd.CommandText = """
                UPDATE character_quests SET completed = TRUE
                WHERE character_id = @id AND quest_id = @quest AND kills >= @required AND completed = FALSE
                """;
            cmd.Parameters.AddWithValue("@quest", quest.Id);
            cmd.Parameters.AddWithValue("@required", quest.KillCount == 0 ? 1 : quest.KillCount);
            if (cmd.ExecuteNonQuery() != 1)
                return null;

            CharacterItem? rewardInventoryItem = null;
            if (rewardItem != 0 && quest.RewardItemCount != 0)
            {
                var inventory = ReadInventory(cmd, characterId, forUpdate: true);
                var stack = inventory.FirstOrDefault(item => item.ItemId == rewardItem
                    && item.BackpackIndex < backpackCapacity && item.Quantity <= ushort.MaxValue - quest.RewardItemCount);
                if (stack is not null)
                {
                    rewardInventoryItem = stack with { Quantity = (ushort)(stack.Quantity + quest.RewardItemCount) };
                    UpdateItemQuantity(cmd, characterId, rewardInventoryItem);
                }
                else
                {
                    if (FindEmptyBackpackCell(inventory) is not { } cell)
                        return null;
                    rewardInventoryItem = InsertBackpackItem(cmd, characterId, inventory, rewardItem, cell,
                        quest.RewardItemCount);
                }
            }

            int newBalance = checked(balance + quest.Reward);
            WriteSilver(cmd, characterId, newBalance);
            transaction.Commit();
            return new QuestCompletionResult(newBalance, rewardInventoryItem);
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось завершить задание {quest.Id} персонажа {characterId}. Код MySQL {ex.Number}.");
            return null;
        }
    }

    internal static (int Silver, IReadOnlyList<CharacterItem> Items)? TryBuy(int characterId, ShopItem stock, ushort quantity)
    {
        if (quantity == 0 || quantity > stock.StackLimit)
            return null;

        try
        {
            using var db = Database.Open();
            using var transaction = db.BeginTransaction();
            using var cmd = db.CreateCommand();
            cmd.Transaction = transaction;
            int cost = stock.Price * quantity;
            if (LockSilver(cmd, characterId) is not { } balance || balance < cost)
                return null;

            var inventory = ReadInventory(cmd, characterId, forUpdate: true);
            var updates = new List<CharacterItem>();
            int remaining = quantity;
            foreach (var stack in inventory.Where(item => item.ItemId == stock.ItemId && item.Quantity < stock.StackLimit))
            {
                int added = Math.Min(remaining, stock.StackLimit - stack.Quantity);
                var updated = stack with { Quantity = (ushort)(stack.Quantity + added) };
                UpdateItemQuantity(cmd, characterId, updated);
                updates.Add(updated);
                remaining -= added;
                if (remaining == 0)
                    break;
            }
            if (remaining > 0)
            {
                if (FindEmptyBackpackCell(inventory) is not { } cell)
                    return null;
                updates.Add(InsertBackpackItem(cmd, characterId, inventory, stock.ItemId, cell, (ushort)remaining));
            }

            int newBalance = balance - cost;
            WriteSilver(cmd, characterId, newBalance);
            transaction.Commit();
            return (newBalance, updates);
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось провести покупку персонажа {characterId}. Код MySQL {ex.Number}.");
            return null;
        }
    }

    private static int? LockSilver(MySqlCommand cmd, int characterId)
    {
        cmd.Parameters.AddWithValue("@id", characterId);
        cmd.CommandText = "SELECT silver FROM characters WHERE id = @id FOR UPDATE";
        var value = cmd.ExecuteScalar();
        return value is null || value == DBNull.Value ? null : Convert.ToInt32(value);
    }

    private static void WriteSilver(MySqlCommand cmd, int characterId, int balance)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = "UPDATE characters SET silver = @silver WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", characterId);
        cmd.Parameters.AddWithValue("@silver", balance);
        cmd.ExecuteNonQuery();
    }
}
