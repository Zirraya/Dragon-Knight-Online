using DragonKnightServer.Database;
using DragonKnightServer.Protocol;

namespace DragonKnightServer.Handlers;

public static class LoginHandler
{
    // Разбор реального 69-байтового пакета логина (кадр 11098 из дампа):
    //   00 45                 длина = 69 (BIG-ENDIAN!)
    //   00 10 00 00 10 09     заголовок/версия протокола (назначение полей пока неизвестно)
    //   00 07                 (перед строкой логина)
    //   "harpie\0"            логин
    //   00
    //   "!<md5>\0"            пароль: '!' + MD5-хэш (32 hex), клиент НЕ шлёт открытый пароль
    //   00 0d
    //   "2c6dc19b785f\0"      MAC-адрес клиента (2c:6d:c1:9b:78:5f)
    //   56 1e
    public static async Task Handle(ClientSession client, byte[] packet, int size)
    {
        try
        {
            Console.WriteLine($"Обработка логин пакета, размер: {size} байт");

            var r = new PacketReader(packet, size);

            ushort length = r.ReadUShort();   // теперь big-endian => 69
            ushort field1 = r.ReadUShort();   // 0x0010
            r.ReadUShort();                    // 0x0000
            r.ReadUShort();                    // 0x1009
            r.ReadUShort();                    // 0x0007

            Console.WriteLine($"Длина: {length}, поле1: 0x{field1:X4}");

            string user = r.ReadString();      // "harpie"
            r.Skip(1);                          // разделительный 0x00 перед паролем
            string passField = r.ReadString(); // "!<md5>"

            // Убираем ведущий '!'. В БД храним именно то, что шлёт клиент (MD5-хэш),
            // потому что открытого пароля у нас нет и быть не должно.
            string passHash = passField.StartsWith("!") ? passField.Substring(1) : passField;

            Console.WriteLine($"Логин: '{user}', хэш пароля: '{passHash}'");

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

            client.AccountId = account;
            Console.WriteLine($"Успешный вход: {user} (ID: {account})");

            // TODO: реальный сервер отвечает пакетом 260 байт (кадр 3318).
            // Его содержимое в текстовых логах отсутствует (там только заголовки),
            // поэтому точный ответ нужно снять из payload-дампа.
            await client.SendServerList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в LoginHandler: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private static async Task SendLoginError(ClientSession client)
    {
        // 4-байтовый ответ с длиной в BIG-ENDIAN (00 04)
        byte[] errorPacket = { 0x00, 0x04, 0x00, 0x0B };
        await client.Send(errorPacket);
    }
}
