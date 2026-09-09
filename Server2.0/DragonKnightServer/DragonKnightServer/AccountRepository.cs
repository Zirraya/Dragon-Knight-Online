using MySqlConnector;

namespace DragonKnightServer.Database;

public class AccountRepository
{
    public static int Login(string user, string pass)
    {
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            Console.WriteLine("Пустой логин или пароль");
            return -1;
        }

        try
        {
            using var db = Database.Open();
            var cmd = db.CreateCommand();

            // Ищем пользователя
            cmd.CommandText = "SELECT id, password FROM accounts WHERE username=@u";
            cmd.Parameters.AddWithValue("@u", user);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                string storedPass = reader.GetString("password");
                if (storedPass == pass)
                {
                    int id = reader.GetInt32("id");
                    Console.WriteLine($"Найден пользователь: {user} (ID: {id})");
                    return id;
                }
                else
                {
                    Console.WriteLine($"Неверный пароль для {user}");
                    return -1;
                }
            }

            // Создаем нового пользователя (авто-регистрация при первом входе).
            reader.Close();

            // БЫЛО: "INSERT ...; SELECT LAST_INSERT_ID();" через ExecuteScalar().
            // ExecuteScalar читает первую ячейку ПЕРВОГО результата (INSERT), а он
            // строк не возвращает => всегда 0 => вход считался неудачным. Исправлено:
            // делаем INSERT через ExecuteNonQuery и берём cmd.LastInsertedId.
            cmd.CommandText = "INSERT INTO accounts(username,password) VALUES(@u,@p)";
            cmd.Parameters.AddWithValue("@p", pass);
            cmd.ExecuteNonQuery();

            int newId = (int)cmd.LastInsertedId;
            Console.WriteLine($"Создан новый пользователь: {user} (ID: {newId})");
            return newId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка базы данных: {ex.Message}");
            return -1;
        }
    }
}