using DragonKnightServer.Database;
using DragonKnightServer.Protocol;

namespace DragonKnightServer.Handlers;

public static class LoginHandler
{
    public static async Task Handle(ClientSession client, byte[] packet, int size)
    {
        client.AccountId = 0;
        try
        {
            Console.WriteLine($"Обработка логин пакета, размер: {size} байт");

            var r = new PacketReader(packet, size);

            r.Skip(LoginPackets.HeaderSize + sizeof(ushort));
            string user = r.ReadString();
            r.Skip(1); // Дополнительный нулевой байт между строками.
            string passField = r.ReadString();

            // Убираем ведущий '!'. В БД храним именно то, что шлёт клиент (MD5-хэш),
            // потому что открытого пароля у нас нет и быть не должно.
            string passHash = passField.StartsWith("!") ? passField.Substring(1) : passField;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(passHash))
            {
                Console.WriteLine("Не удалось разобрать логин/пароль из пакета");
                await SendLoginError(client);
                return;
            }

            int account = AccountRepository.Login(user, passHash);

            if (account <= 0)
            {
                Console.WriteLine($"Ошибка входа для {user}");
                await SendLoginError(client);
                return;
            }

            var characters = CharacterRepository.GetForLogin(account, LoginPackets.CharacterSlots);
            client.AccountId = account;
            Console.WriteLine($"Успешный вход: {user} (ID: {account})");

            await client.SendCharacterList(characters);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в LoginHandler: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            await SendLoginError(client);
        }
    }

    private static async Task SendLoginError(ClientSession client)
    {
        await client.Send(LoginPackets.CreateResult(Opcode.CharacterList, 1));
        client.CloseAfterResponse();
    }
}
