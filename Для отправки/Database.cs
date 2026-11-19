using MySqlConnector;

namespace DragonKnightServer.Database;

public static class Database
{
    // Поменяйте пароль на ваш
    private static string connectionString =
        "Server=localhost;" +
        "Database=dragonknightonline;" +
        "User=root;" +
        "Password=idontknow;" +
        "Port=3306;";

    public static MySqlConnection Open()
    {
        var connection = new MySqlConnection(connectionString);
        connection.Open();
        return connection;
    }
}