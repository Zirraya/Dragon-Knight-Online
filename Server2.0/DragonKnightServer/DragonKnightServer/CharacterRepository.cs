using MySqlConnector;


namespace DragonKnightServer.Database;

public record LoginCharacter(string Name, int Level, int Map, int Template, byte[]? Appearance);

internal record WorldCharacter(int Id, string Name, int Level, int Map, int Template, byte[]? Appearance,
    float X, float Y, float Z, int HitPoints, int Mana, int Experience)
{
    public IReadOnlyList<CharacterItem> Inventory { get; init; } = [];
    public byte[]? Quickbar { get; init; }
}

internal record CharacterItem(ushort Slot, ushort ItemId, byte? DragonSlot = null, ushort? BackpackSlot = null)
{
    public ushort BackpackIndex => BackpackSlot ?? Slot;
}

public class CharacterRepository
{
    private static readonly object SchemaLock = new();
    private static bool characterColumnsReady;
    private static bool inventoryTableReady;
    internal const ushort YoungSilverDragonItem = 58508;
    internal const ushort ElvenDragonItem = 41033;
    internal const int TestLevel = 15;

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
        cmd.CommandText = """
            INSERT INTO characters (account_id, name, class_id, wire_appearance)
            VALUES (@accountId, @name, @template, @appearance)
            """;
        cmd.Parameters.AddWithValue("@template", template);
        cmd.Parameters.Add("@appearance", MySqlDbType.VarBinary).Value = appearance;
        cmd.ExecuteNonQuery();

        GrantStarterItems(cmd, cmd.LastInsertedId);
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
                   COALESCE(hp, 100), COALESCE(mp, 100), COALESCE(exp, 0), quickbar
            FROM characters
            WHERE account_id = @accountId AND name = @name
            ORDER BY id
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.Parameters.AddWithValue("@name", name);

        WorldCharacter character;
        using (var reader = cmd.ExecuteReader())
        {
            if (!reader.Read())
                return null;

            character = new WorldCharacter(reader.GetInt32(0), reader.GetString(1), Math.Max(TestLevel, reader.GetInt32(2)),
                reader.GetInt32(3), reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetFieldValue<byte[]>(5),
                reader.GetFloat(6), reader.GetFloat(7), reader.GetFloat(8),
                reader.GetInt32(9), reader.GetInt32(10), reader.GetInt32(11))
            {
                Quickbar = reader.IsDBNull(12) ? null : reader.GetFieldValue<byte[]>(12)
            };
        }

        EnsureInventoryTable(db);
        GrantStarterItems(cmd, character.Id);
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT slot, item_id, dragon_slot, backpack_slot FROM character_items WHERE character_id = @id ORDER BY slot";
        cmd.Parameters.AddWithValue("@id", character.Id);
        var inventory = new List<CharacterItem>();
        using var items = cmd.ExecuteReader();
        while (items.Read())
            inventory.Add(new CharacterItem(items.GetUInt16(0), items.GetUInt16(1),
                items.IsDBNull(2) ? null : items.GetByte(2), items.IsDBNull(3) ? null : items.GetUInt16(3)));

        return character with { Inventory = inventory };
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
            cmd.CommandText = """
                SELECT slot, COALESCE(backpack_slot, slot) FROM character_items
                WHERE character_id = @id FOR UPDATE
                """;
            cmd.Parameters.AddWithValue("@id", characterId);
            ushort? currentSource = null;
            ushort? currentTarget = null;
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    ushort location = reader.GetUInt16(1);
                    if (location == source.BackpackIndex)
                        currentSource = reader.GetUInt16(0);
                    if (location == destination)
                        currentTarget = reader.GetUInt16(0);
                }
            }

            if (currentSource != source.Slot || currentTarget != target?.Slot)
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
                    PRIMARY KEY (character_id, slot),
                    FOREIGN KEY (character_id) REFERENCES characters (id) ON DELETE CASCADE
                )
                """;
            cmd.ExecuteNonQuery();
            EnsureColumn(db, "character_items", "dragon_slot", "TINYINT UNSIGNED NULL");
            EnsureColumn(db, "character_items", "backpack_slot", "SMALLINT UNSIGNED NULL");
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
