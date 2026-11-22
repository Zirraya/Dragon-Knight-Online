using MySqlConnector;


namespace DragonKnightServer.Database;

public record LoginCharacter(string Name, int Level, int Map, int Template, byte[]? Appearance);

internal record WorldCharacter(int Id, string Name, int Level, int Map, int Template, byte[]? Appearance,
    float X, float Y, float Z, int HitPoints, int Mana, int Experience)
{
    public IReadOnlyList<CharacterItem> Inventory { get; init; } = [];
    public byte[]? Quickbar { get; init; }
    public int Silver { get; init; }
    public IReadOnlyDictionary<int, QuestProgress> Quests { get; init; } = new Dictionary<int, QuestProgress>();
}

internal record CharacterItem(ushort Slot, ushort ItemId, byte? DragonSlot = null, ushort? BackpackSlot = null,
    ushort Quantity = 1, byte? EquipmentSlot = null)
{
    public ushort BackpackIndex => BackpackSlot ?? (EquipmentSlot.HasValue ? ushort.MaxValue : Slot);
    public bool IsEquipped => EquipmentSlot.HasValue && !BackpackSlot.HasValue;
}

internal record EquipmentToggleResult(CharacterItem Item, bool Equipped);

public partial class CharacterRepository
{
    private static readonly object SchemaLock = new();
    private static bool characterColumnsReady;
    private static bool inventoryTableReady;
    internal const ushort YoungSilverDragonItem = 58508;
    internal const ushort ElvenDragonItem = 41033;
    internal const ushort StarterDragonEgg = 58523;
    internal const int TestLevel = 85;

    internal static bool Delete(string username, string characterName)
    {
        using var db = Database.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            DELETE FROM characters
            WHERE account_id = (SELECT id FROM accounts WHERE BINARY username = BINARY @owner)
              AND BINARY name = BINARY @name
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@owner", username);
        cmd.Parameters.AddWithValue("@name", characterName);
        return cmd.ExecuteNonQuery() == 1;
    }

    internal static bool TryDelete(int accountId, string? name)
    {
        if (accountId <= 0 || string.IsNullOrWhiteSpace(name))
            return false;

        try
        {
            using var db = Database.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "DELETE FROM characters WHERE account_id = @accountId AND BINARY name = BINARY @name LIMIT 1";
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@name", name);
            return cmd.ExecuteNonQuery() == 1;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось удалить персонажа аккаунта {accountId}. Код MySQL {ex.Number}.");
            return false;
        }
    }

    internal static bool TrySavePosition(int accountId, int characterId, int map, (int X, int Y, int Z) position)
    {
        try
        {
            using var db = Database.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = """
                UPDATE characters SET map = @map, x = @x, y = @y, z = @z
                WHERE account_id = @accountId AND id = @id
                """;
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@id", characterId);
            cmd.Parameters.AddWithValue("@map", map);
            cmd.Parameters.AddWithValue("@x", position.X);
            cmd.Parameters.AddWithValue("@y", position.Y);
            cmd.Parameters.AddWithValue("@z", position.Z);
            return cmd.ExecuteNonQuery() == 1;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось сохранить позицию персонажа {characterId}. Код MySQL {ex.Number}.");
            return false;
        }
    }

    public static IReadOnlyList<LoginCharacter> GetForLogin(int accountId, int limit)
    {
        using var db = Database.Open();
        EnsureCharacterColumns(db);
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            SELECT name, COALESCE(level, 1), COALESCE(map, 1),
                   COALESCE(class_id, 1), wire_appearance
            FROM characters
            WHERE account_id = @accountId
            ORDER BY id
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.Parameters.AddWithValue("@limit", limit);

        var characters = new List<LoginCharacter>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            characters.Add(new LoginCharacter(reader.GetString(0), Math.Max(TestLevel, reader.GetInt32(1)),
                reader.GetInt32(2), reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetFieldValue<byte[]>(4)));
        }

        return characters;
    }

    public static bool TryCreate(int accountId, string name, int template, byte[] appearance, int limit)
    {
        using var db = Database.Open();
        EnsureCharacterColumns(db);
        EnsureInventoryTable(db);
        using var transaction = db.BeginTransaction();
        using var cmd = db.CreateCommand();
        cmd.Transaction = transaction;

        // Последовательное создание для одного аккаунта защищает проверку лимита и имени.
        cmd.CommandText = "SELECT id FROM accounts WHERE id = @accountId FOR UPDATE";
        cmd.Parameters.AddWithValue("@accountId", accountId);
        if (cmd.ExecuteScalar() is null)
            return false;

        cmd.CommandText = """
            SELECT COUNT(*) < @limit AND COUNT(CASE WHEN name = @name THEN 1 END) = 0
            FROM characters
            WHERE account_id = @accountId
            """;
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@limit", limit);
        if (!Convert.ToBoolean(cmd.ExecuteScalar()))
            return false;

        cmd.Parameters.RemoveAt("@limit");
        var origin = CharacterOrigins.ForFaction(CharacterOrigins.ReadFaction(appearance));
        cmd.CommandText = """
            INSERT INTO characters (account_id, name, class_id, wire_appearance, map, x, y, z)
            VALUES (@accountId, @name, @template, @appearance, 1, @x, @y, @z)
            """;
        cmd.Parameters.AddWithValue("@template", template);
        cmd.Parameters.Add("@appearance", MySqlDbType.VarBinary).Value = appearance;
        cmd.Parameters.AddWithValue("@x", origin.X);
        cmd.Parameters.AddWithValue("@y", origin.Y);
        cmd.Parameters.AddWithValue("@z", origin.Z);
        cmd.ExecuteNonQuery();

        int characterId = checked((int)cmd.LastInsertedId);
        GrantStarterItems(cmd, characterId);
        GrantStarterEquipment(cmd, characterId, template);
        GrantStarterEgg(cmd, characterId);
        transaction.Commit();
        return true;
    }

    internal static WorldCharacter? FindForWorld(int accountId, string name)
    {
        using var db = Database.Open();
        EnsureCharacterColumns(db);
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, COALESCE(level, 1), COALESCE(map, 1), COALESCE(class_id, 1),
                   wire_appearance, COALESCE(x, 100), COALESCE(y, 200), COALESCE(z, 0),
                   COALESCE(hp, 100), COALESCE(mp, 100), COALESCE(exp, 0), quickbar, silver, starter_egg_granted
            FROM characters
            WHERE account_id = @accountId AND name = @name
            ORDER BY id
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.Parameters.AddWithValue("@name", name);

        WorldCharacter character;
        bool eggGranted;
        using (var reader = cmd.ExecuteReader())
        {
            if (!reader.Read())
                return null;

            character = new WorldCharacter(reader.GetInt32(0), reader.GetString(1), Math.Max(TestLevel, reader.GetInt32(2)),
                reader.GetInt32(3), reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetFieldValue<byte[]>(5),
                reader.GetFloat(6), reader.GetFloat(7), reader.GetFloat(8),
                reader.GetInt32(9), reader.GetInt32(10), reader.GetInt32(11))
            {
                Quickbar = reader.IsDBNull(12) ? null : reader.GetFieldValue<byte[]>(12),
                Silver = reader.GetInt32(13)
            };
            eggGranted = reader.GetBoolean(14);
        }

        EnsureInventoryTable(db);
        EnsureQuestTable(db);
        GrantStarterItems(cmd, character.Id);
        GrantStarterEquipment(cmd, character.Id, character.Template);
        if (!eggGranted)
        {
            using var transaction = db.BeginTransaction();
            cmd.Transaction = transaction;
            GrantStarterEgg(cmd, character.Id);
            transaction.Commit();
            cmd.Transaction = null;
        }
        return character with { Inventory = ReadInventory(cmd, character.Id), Quests = ReadQuests(cmd, character.Id) };
    }

    internal static bool TrySaveQuickbar(int characterId, byte[] data)
    {
        try
        {
            using var db = Database.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "UPDATE characters SET quickbar = @data WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", characterId);
            cmd.Parameters.Add("@data", MySqlDbType.VarBinary).Value = data;
            return cmd.ExecuteNonQuery() == 1;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось сохранить панель персонажа {characterId}. Код MySQL {ex.Number}.");
            return false;
        }
    }

    internal static bool TryMoveBackpackItem(int characterId, CharacterItem source, ushort destination, CharacterItem? target)
    {
        try
        {
            using var db = Database.Open();
            using var transaction = db.BeginTransaction();
            using var cmd = db.CreateCommand();
            cmd.Transaction = transaction;
            var items = ReadInventory(cmd, characterId, forUpdate: true);
            var currentSource = items.Find(item => item.BackpackIndex == source.BackpackIndex);
            var currentTarget = items.Find(item => item.BackpackIndex == destination);
            if (currentSource != source || currentTarget != target)
                return false;

            // Ячейки меняются в одной транзакции, ключи экземпляров остаются прежними.
            cmd.CommandText = """
                UPDATE character_items
                SET backpack_slot = CASE WHEN slot = @source THEN @to ELSE @from END
                WHERE character_id = @id AND slot IN (@source, @target)
                """;
            cmd.Parameters.AddWithValue("@source", source.Slot);
            cmd.Parameters.AddWithValue("@target", (object?)target?.Slot ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@from", source.BackpackIndex);
            cmd.Parameters.AddWithValue("@to", destination);
            cmd.ExecuteNonQuery();
            transaction.Commit();
            return true;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось переместить предмет персонажа {characterId}. Код MySQL {ex.Number}.");
            return false;
        }
    }

    internal static EquipmentToggleResult? TryToggleEquipment(int characterId, byte equipmentSlot, int capacity)
    {
        if (equipmentSlot == 0 || capacity <= 0)
            return null;

        try
        {
            using var db = Database.Open();
            EnsureInventoryTable(db);
            using var transaction = db.BeginTransaction();
            using var cmd = db.CreateCommand();
            cmd.Transaction = transaction;
            var items = ReadInventory(cmd, characterId, forUpdate: true);
            var equipped = items.Find(item => item.EquipmentSlot == equipmentSlot && item.IsEquipped);
            CharacterItem? updated;
            bool isEquipped;

            if (equipped is not null)
            {
                var occupied = items.Where(item => item.BackpackIndex < capacity)
                    .Select(item => item.BackpackIndex).ToHashSet();
                int destination = Enumerable.Range(0, capacity)
                    .FirstOrDefault(index => !occupied.Contains((ushort)index), -1);
                if (destination < 0)
                    return null;

                updated = equipped with { BackpackSlot = (ushort)destination };
                isEquipped = false;
            }
            else
            {
                var backpackItem = items.Find(item => item.EquipmentSlot == equipmentSlot
                    && item.BackpackSlot.HasValue);
                if (backpackItem is null)
                    return null;

                updated = backpackItem with { BackpackSlot = null };
                isEquipped = true;
            }

            cmd.Parameters.Clear();
            cmd.CommandText = """
                UPDATE character_items
                SET equipment_slot = @equipmentSlot, backpack_slot = @backpackSlot
                WHERE character_id = @characterId AND slot = @slot
                """;
            cmd.Parameters.AddWithValue("@equipmentSlot", (object?)updated.EquipmentSlot ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@backpackSlot", (object?)updated.BackpackSlot ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@characterId", characterId);
            cmd.Parameters.AddWithValue("@slot", updated.Slot);
            if (cmd.ExecuteNonQuery() != 1)
                return null;

            transaction.Commit();
            return new EquipmentToggleResult(updated, isEquipped);
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось переключить экипировку персонажа {characterId}. Код MySQL {ex.Number}.");
            return null;
        }
    }

    internal static CharacterItem? TryStoreLoot(int characterId, LootDrop drop, int capacity)
    {
        try
        {
            using var db = Database.Open();
            using var transaction = db.BeginTransaction();
            using var cmd = db.CreateCommand();
            cmd.Transaction = transaction;
            var items = ReadInventory(cmd, characterId, forUpdate: true);
            var stack = items.Find(item => item.ItemId == drop.ItemId && item.Quantity < drop.StackLimit);
            if (stack is not null)
            {
                stack = stack with { Quantity = (ushort)(stack.Quantity + 1) };
                UpdateItemQuantity(cmd, characterId, stack);
            }
            else
            {
                var occupied = items.Select(item => item.BackpackIndex).ToHashSet();
                int cell = Enumerable.Range(0, capacity).FirstOrDefault(index => !occupied.Contains((ushort)index), -1);
                if (cell < 0)
                {
                    Console.WriteLine($"Рюкзак персонажа {characterId} заполнен. Предмет {drop.ItemId} не помещается.");
                    return null;
                }
                stack = InsertBackpackItem(cmd, characterId, items, drop.ItemId, (ushort)cell, 1);
            }

            transaction.Commit();
            return stack;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось сохранить добычу персонажа {characterId}. Код MySQL {ex.Number}.");
            return null;
        }
    }

    internal static CharacterItem? TrySplitBackpackStack(int characterId, CharacterItem source, ushort destination,
        ushort quantity)
    {
        if (quantity == 0 || quantity >= source.Quantity)
            return null;

        try
        {
            using var db = Database.Open();
            using var transaction = db.BeginTransaction();
            using var cmd = db.CreateCommand();
            cmd.Transaction = transaction;
            var items = ReadInventory(cmd, characterId, forUpdate: true);
            if (!items.Contains(source) || items.Exists(item => item.BackpackIndex == destination))
                return null;

            var split = InsertBackpackItem(cmd, characterId, items, source.ItemId, destination, quantity);
            UpdateItemQuantity(cmd, characterId, source with { Quantity = (ushort)(source.Quantity - quantity) });
            transaction.Commit();
            return split;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось разделить стопку персонажа {characterId}. Код MySQL {ex.Number}.");
            return null;
        }
    }

    private static List<CharacterItem> ReadInventory(MySqlCommand cmd, int characterId, bool forUpdate = false)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = """
            SELECT slot, item_id, dragon_slot, backpack_slot, quantity
                   , equipment_slot
            FROM character_items WHERE character_id = @id ORDER BY slot
            """ + (forUpdate ? " FOR UPDATE" : "");
        cmd.Parameters.AddWithValue("@id", characterId);
        using var reader = cmd.ExecuteReader();
        var items = new List<CharacterItem>();
        while (reader.Read())
            items.Add(new CharacterItem(reader.GetUInt16(0), reader.GetUInt16(1),
                reader.IsDBNull(2) ? null : reader.GetByte(2), reader.IsDBNull(3) ? null : reader.GetUInt16(3),
                reader.GetUInt16(4), reader.IsDBNull(5) ? null : reader.GetByte(5)));
        return items;
    }

    private static CharacterItem InsertBackpackItem(MySqlCommand cmd, int characterId, List<CharacterItem> items,
        ushort itemId, ushort destination, ushort quantity)
    {
        var usedIds = items.Select(item => item.Slot).ToHashSet();
        ushort slot = (ushort)Enumerable.Range(2, ushort.MaxValue - 1).First(id => !usedIds.Contains((ushort)id));
        cmd.Parameters.Clear();
        cmd.CommandText = """
            INSERT INTO character_items (character_id, slot, item_id, backpack_slot, quantity)
            VALUES (@id, @slot, @item, @destination, @quantity)
            """;
        cmd.Parameters.AddWithValue("@id", characterId);
        cmd.Parameters.AddWithValue("@slot", slot);
        cmd.Parameters.AddWithValue("@item", itemId);
        cmd.Parameters.AddWithValue("@destination", destination);
        cmd.Parameters.AddWithValue("@quantity", quantity);
        cmd.ExecuteNonQuery();
        return new CharacterItem(slot, itemId, BackpackSlot: destination, Quantity: quantity);
    }

    private static void UpdateItemQuantity(MySqlCommand cmd, int characterId, CharacterItem item)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = "UPDATE character_items SET quantity = @quantity WHERE character_id = @id AND slot = @slot";
        cmd.Parameters.AddWithValue("@id", characterId);
        cmd.Parameters.AddWithValue("@slot", item.Slot);
        cmd.Parameters.AddWithValue("@quantity", item.Quantity);
        cmd.ExecuteNonQuery();
    }

    internal static bool SaveDragonSlot(int characterId, CharacterItem item)
    {
        try
        {
            using var db = Database.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = """
                UPDATE character_items SET dragon_slot = @dragonSlot
                WHERE character_id = @characterId AND slot = @slot AND item_id = @item
                """;
            cmd.Parameters.AddWithValue("@characterId", characterId);
            cmd.Parameters.AddWithValue("@slot", item.Slot);
            cmd.Parameters.AddWithValue("@item", item.ItemId);
            cmd.Parameters.Add("@dragonSlot", MySqlDbType.UByte).Value = (object?)item.DragonSlot ?? DBNull.Value;
            return cmd.ExecuteNonQuery() == 1;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось сохранить слот дракона для персонажа {characterId}. Код MySQL {ex.Number}.");
            return false;
        }
    }

    private static void GrantStarterItems(MySqlCommand cmd, long characterId)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = """
            INSERT INTO character_items (character_id, slot, item_id)
            VALUES (@characterId, 0, @silver), (@characterId, 1, @elven)
            ON DUPLICATE KEY UPDATE slot = character_items.slot
            """;
        cmd.Parameters.AddWithValue("@characterId", characterId);
        cmd.Parameters.AddWithValue("@silver", YoungSilverDragonItem);
        cmd.Parameters.AddWithValue("@elven", ElvenDragonItem);
        cmd.ExecuteNonQuery();
    }

    private static void GrantStarterEquipment(MySqlCommand cmd, long characterId, int template)
    {
        var definitions = StarterEquipment.ForTemplate(template);
        if (definitions.Count == 0)
            return;

        var existing = ReadInventory(cmd, checked((int)characterId));
        var usedSlots = existing.Select(item => item.Slot).ToHashSet();
        ushort nextSlot = (ushort)Math.Max(2, existing.Select(item => (int)item.Slot)
            .DefaultIfEmpty(1).Max() + 1);

        foreach (var definition in definitions)
        {
            if (existing.Any(item => item.EquipmentSlot == definition.EquipmentSlot))
                continue;

            while (usedSlots.Contains(nextSlot))
                nextSlot = checked((ushort)(nextSlot + 1));

            cmd.Parameters.Clear();
            cmd.CommandText = """
                INSERT INTO character_items
                    (character_id, slot, item_id, equipment_slot, quantity)
                VALUES (@characterId, @slot, @itemId, @equipmentSlot, 1)
                """;
            cmd.Parameters.AddWithValue("@characterId", characterId);
            cmd.Parameters.AddWithValue("@slot", nextSlot);
            cmd.Parameters.AddWithValue("@itemId", definition.ItemId);
            cmd.Parameters.AddWithValue("@equipmentSlot", definition.EquipmentSlot!.Value);
            cmd.ExecuteNonQuery();

            usedSlots.Add(nextSlot);
            existing.Add(definition with { Slot = nextSlot });
            nextSlot = checked((ushort)(nextSlot + 1));
        }
    }

    private static void EnsureInventoryTable(MySqlConnection db)
    {
        lock (SchemaLock)
        {
            if (inventoryTableReady)
                return;

            using var cmd = db.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS character_items
                (
                    character_id INT NOT NULL,
                    slot SMALLINT UNSIGNED NOT NULL,
                    item_id SMALLINT UNSIGNED NOT NULL,
                    dragon_slot TINYINT UNSIGNED NULL,
                    backpack_slot SMALLINT UNSIGNED NULL,
                    equipment_slot TINYINT UNSIGNED NULL,
                    quantity SMALLINT UNSIGNED NOT NULL DEFAULT 1,
                    PRIMARY KEY (character_id, slot),
                    FOREIGN KEY (character_id) REFERENCES characters (id) ON DELETE CASCADE
                )
                """;
            cmd.ExecuteNonQuery();
            EnsureColumn(db, "character_items", "dragon_slot", "TINYINT UNSIGNED NULL");
            EnsureColumn(db, "character_items", "backpack_slot", "SMALLINT UNSIGNED NULL");
            EnsureColumn(db, "character_items", "quantity", "SMALLINT UNSIGNED NOT NULL DEFAULT 1");
            EnsureColumn(db, "character_items", "equipment_slot", "TINYINT UNSIGNED NULL");
            inventoryTableReady = true;
        }
    }

    private static void EnsureCharacterColumns(MySqlConnection db)
    {
        lock (SchemaLock)
        {
            if (characterColumnsReady)
                return;

            EnsureColumn(db, "characters", "wire_appearance", "VARBINARY(76) NULL");
            EnsureColumn(db, "characters", "quickbar", "VARBINARY(4096) NULL");
            EnsureColumn(db, "characters", "silver", "INT NOT NULL DEFAULT 0");
            EnsureColumn(db, "characters", "starter_egg_granted", "BOOLEAN NOT NULL DEFAULT FALSE");
            characterColumnsReady = true;
        }
    }

    private static void EnsureColumn(MySqlConnection db, string table, string column, string definition)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND COLUMN_NAME = @column
            """;
        cmd.Parameters.AddWithValue("@table", table);
        cmd.Parameters.AddWithValue("@column", column);
        if (Convert.ToInt32(cmd.ExecuteScalar()) != 0)
            return;

        cmd.Parameters.Clear();
        cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
        try
        {
            cmd.ExecuteNonQuery();
        }
        catch (MySqlException ex) when (ex.Number == 1060)
        {
            // Другой процесс уже добавил столбец.
        }
    }
}
