using MySqlConnector;


namespace DragonKnightServer.Database;


public class CharacterRepository
{


    public static void Create(
    int account,
    string name,
    int cls,
    int gender)
    {


        using var db = Database.Open();


        var cmd = db.CreateCommand();


        cmd.CommandText =
        """
INSERT INTO characters
(
account_id,
name,
class_id,
gender
)
VALUES
(
@a,@n,@c,@g
)
""";


        cmd.Parameters.AddWithValue("@a", account);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@c", cls);
        cmd.Parameters.AddWithValue("@g", gender);


        cmd.ExecuteNonQuery();

    }



}