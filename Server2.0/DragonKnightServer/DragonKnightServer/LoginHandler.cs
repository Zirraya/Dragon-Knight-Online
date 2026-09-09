using DragonKnightServer.Database;
using DragonKnightServer.Protocol;

namespace DragonKnightServer.Handlers;

public static class LoginHandler
{
    public static async Task Handle(ClientSession client, byte[] packet)
    {
        try
        {
            Console.WriteLine($"Обработка логин пакета, размер: {packet.Length} байт");

            var r = new PacketReader(packet);

            // Читаем заголовок
            ushort length = r.ReadUShort();
            ushort opcode = r.ReadUShort();

            Console.WriteLine($"Длина: {length}, Opcode: 0x{opcode:X4}");

            // Читаем логин и пароль
            string user = r.ReadString();
            string pass = r.ReadString();

            Console.WriteLine($"Логин: '{user}', Пароль: '{pass}'");

            int account = AccountRepository.Login(user, pass);

            if (account <= 0)
            {
                Console.WriteLine($"Ошибка входа для {user}");
                // Отправляем ошибку
                byte[] errorPacket = new byte[4];
                errorPacket[0] = 0x04;
                errorPacket[1] = 0x00;
                errorPacket[2] = 0x0B; // LoginSuccess с ошибкой?
                errorPacket[3] = 0x00;
                await client.Send(errorPacket);
                return;
            }

            client.AccountId = account;
            Console.WriteLine($"Успешный вход: {user} (ID: {account})");

            // Отправляем список серверов (как в пакете #18)
            await client.SendServerList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в LoginHandler: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}