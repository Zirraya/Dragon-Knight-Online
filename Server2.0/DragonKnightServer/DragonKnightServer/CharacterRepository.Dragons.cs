using MySqlConnector;

namespace DragonKnightServer.Database;

public partial class CharacterRepository
{
    internal static CharacterItem? TryFeedDragon(int characterId, CharacterItem egg, CharacterItem food,
        byte growthGain, byte satietyGain)
    {
        if (!IsGrowingDragon(egg.ItemId) || !egg.DragonSlot.HasValue || food.Quantity == 0 || food.Slot == egg.Slot
            || !DragonFeedItems.Contains(food.ItemId))
            return null;
        try
        {
            using var db = Database.Open();
            using var tx = db.BeginTransaction();
            using var cmd = db.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT slot, item_id, quantity, dragon_growth, dragon_satiety, dragon_satiety_updated FROM character_items WHERE character_id = @id AND slot IN (@egg, @food) FOR UPDATE";
            cmd.Parameters.AddWithValue("@id", characterId);
            cmd.Parameters.AddWithValue("@egg", egg.Slot);
            cmd.Parameters.AddWithValue("@food", food.Slot);
            ushort eggItem = 0, foodItem = 0, foodQuantity = 0;
            byte growth = 0, satiety = 100;
            DateTime satietyUpdated = DateTime.UtcNow;
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    ushort slot = reader.GetUInt16(0);
                    if (slot == egg.Slot)
                    {
                        eggItem = reader.GetUInt16(1);
                        growth = reader.IsDBNull(3) ? (byte)0 : reader.GetByte(3);
                        satiety = reader.IsDBNull(4) ? (byte)100 : reader.GetByte(4);
                        satietyUpdated = reader.IsDBNull(5)
                            ? DateTime.UtcNow
                            : DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc);
                    }
                    else if (slot == food.Slot)
                    {
                        foodItem = reader.GetUInt16(1);
                        foodQuantity = reader.GetUInt16(2);
                    }
                }
            }
            if (!IsGrowingDragon(eggItem) || foodItem == 0 || foodQuantity == 0)
                return null;
            long elapsedMinutes = Math.Max(0, (long)(DateTime.UtcNow - satietyUpdated).TotalMinutes);
            satiety = (byte)Math.Max(0, satiety - Math.Min(100, elapsedMinutes));
            var updatedEgg = egg with
            {
                DragonGrowth = (byte)Math.Min(100, growth + growthGain),
                DragonSatiety = (byte)Math.Min(100, satiety + satietyGain),
                DragonSatietyUpdated = DateTime.UtcNow
            };
            cmd.Parameters.Clear();
            cmd.CommandText = "UPDATE character_items SET dragon_growth = @growth, dragon_satiety = @satiety, dragon_satiety_updated = @updatedAt WHERE character_id = @id AND slot = @slot";
            cmd.Parameters.AddWithValue("@growth", updatedEgg.DragonGrowth);
            cmd.Parameters.AddWithValue("@satiety", updatedEgg.DragonSatiety);
            cmd.Parameters.AddWithValue("@updatedAt", updatedEgg.DragonSatietyUpdated!.Value);
            cmd.Parameters.AddWithValue("@id", characterId);
            cmd.Parameters.AddWithValue("@slot", egg.Slot);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            cmd.CommandText = foodQuantity == 1
                ? "DELETE FROM character_items WHERE character_id = @id AND slot = @slot"
                : "UPDATE character_items SET quantity = quantity - 1 WHERE character_id = @id AND slot = @slot";
            cmd.Parameters.AddWithValue("@id", characterId);
            cmd.Parameters.AddWithValue("@slot", food.Slot);
            cmd.ExecuteNonQuery();
            tx.Commit();
            return updatedEgg;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось покормить дракона персонажа {characterId}. Код MySQL {ex.Number}.");
            return null;
        }
    }

    internal static CharacterItem? TryHatchDragon(int characterId, CharacterItem egg, ushort hatchlingItem)
    {
        if (egg.ItemId != StarterDragonEgg || egg.DragonGrowth < 100 || !HatchlingDragonItems.Contains(hatchlingItem))
            return null;
        try
        {
            using var db = Database.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "UPDATE character_items SET item_id = @newItem, dragon_slot = NULL, dragon_growth = 0, dragon_satiety = 100, dragon_satiety_updated = UTC_TIMESTAMP() WHERE character_id = @id AND slot = @slot AND item_id = @egg AND dragon_growth >= 100";
            cmd.Parameters.AddWithValue("@newItem", hatchlingItem);
            cmd.Parameters.AddWithValue("@id", characterId);
            cmd.Parameters.AddWithValue("@slot", egg.Slot);
            cmd.Parameters.AddWithValue("@egg", StarterDragonEgg);
            return cmd.ExecuteNonQuery() == 1 ? egg with { ItemId = hatchlingItem, DragonSlot = null, DragonGrowth = 0,
                DragonSatiety = 100, DragonSatietyUpdated = DateTime.UtcNow } : null;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось вылупить дракона персонажа {characterId}. Код MySQL {ex.Number}.");
            return null;
        }
    }

    internal static CharacterItem? TryEvolveDragon(int characterId, CharacterItem dragon)
    {
        if (dragon.DragonGrowth < 100
            || !DragonEvolutionItems.TryGetValue(dragon.ItemId, out ushort contractItem))
            return null;
        try
        {
            using var db = Database.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = """
                UPDATE character_items
                SET item_id = @newItem, dragon_slot = NULL, dragon_growth = 0,
                    dragon_satiety = 100, dragon_satiety_updated = UTC_TIMESTAMP()
                WHERE character_id = @id AND slot = @slot AND item_id = @oldItem
                  AND dragon_growth >= 100
                """;
            cmd.Parameters.AddWithValue("@newItem", contractItem);
            cmd.Parameters.AddWithValue("@id", characterId);
            cmd.Parameters.AddWithValue("@slot", dragon.Slot);
            cmd.Parameters.AddWithValue("@oldItem", dragon.ItemId);
            return cmd.ExecuteNonQuery() == 1
                ? dragon with { ItemId = contractItem, DragonSlot = null, DragonGrowth = 0,
                    DragonSatiety = 100, DragonSatietyUpdated = DateTime.UtcNow }
                : null;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось эволюционировать дракона персонажа {characterId}. Код MySQL {ex.Number}.");
            return null;
        }
    }
}
