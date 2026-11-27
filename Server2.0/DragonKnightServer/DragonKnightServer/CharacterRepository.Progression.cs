using MySqlConnector;

namespace DragonKnightServer.Database;

internal readonly record struct ExperienceGainResult(int Level, int Experience, int HitPoints, int Mana,
    bool Leveled);

public partial class CharacterRepository
{
    internal static ExperienceGainResult? TryAddExperience(int characterId, int template, int amount)
    {
        if (amount <= 0)
            return null;

        try
        {
            using var db = Database.Open();
            using var transaction = db.BeginTransaction();
            using var cmd = db.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                SELECT COALESCE(level, 1), COALESCE(exp, 0),
                       COALESCE(hp, 100), COALESCE(mp, 100)
                FROM characters WHERE id = @id FOR UPDATE
                """;
            cmd.Parameters.AddWithValue("@id", characterId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            int oldLevel = Math.Clamp(reader.GetInt32(0), 1, CharacterProgression.MaxLevel);
            int oldExperience = Math.Max(0, reader.GetInt32(1));
            int oldHitPoints = Math.Max(0, reader.GetInt32(2));
            int oldMana = Math.Max(0, reader.GetInt32(3));
            reader.Close();

            int experience = checked(oldExperience + amount);
            int level = CharacterProgression.LevelForExperience(experience);
            var stats = CharacterProgression.For(template, level);
            int hitPoints = level > oldLevel ? stats.HitPoints : oldHitPoints;
            int mana = level > oldLevel ? stats.Mana : oldMana;
            cmd.Parameters.Clear();
            cmd.CommandText = """
                UPDATE characters
                SET level = @level, exp = @exp, hp = @hp, mp = @mp
                WHERE id = @id
                """;
            cmd.Parameters.AddWithValue("@id", characterId);
            cmd.Parameters.AddWithValue("@level", level);
            cmd.Parameters.AddWithValue("@exp", experience);
            cmd.Parameters.AddWithValue("@hp", hitPoints);
            cmd.Parameters.AddWithValue("@mp", mana);
            if (cmd.ExecuteNonQuery() != 1)
                return null;

            transaction.Commit();
            return new ExperienceGainResult(level, experience, hitPoints, mana, level > oldLevel);
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Не удалось начислить опыт персонажу {characterId}. Код MySQL {ex.Number}.");
            return null;
        }
        catch (OverflowException)
        {
            Console.WriteLine($"Опыт персонажа {characterId} достиг максимального значения.");
            return null;
        }
    }
}
