using System.Net;
using System.Net.Sockets;
using System.Text;
using DragonKnightServer.Database;

namespace DragonKnightServer;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        if (args.Length > 0)
        {
            if (args is ["--reset-local-password", var username])
                return LocalPasswordReset.Run(username);

            if (args is ["--delete-local-character", var owner, var name])
            {
                if (Console.IsInputRedirected)
                    return 1;
                Console.WriteLine($"Удалить персонажа «{name}» на аккаунте «{owner}»? Для подтверждения введи имя персонажа.");
                if (!string.Equals(Console.ReadLine(), name, StringComparison.Ordinal))
                    return 1;

                try
                {
                    bool deleted = CharacterRepository.Delete(owner, name);
                    Console.WriteLine(deleted ? "Персонаж удалён." : "На этом аккаунте нет персонажа с таким именем.");
                    return deleted ? 0 : 1;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Удаление не выполнено: {ex.Message}");
                    return 1;
                }
            }

            Console.Error.WriteLine("Использование: --reset-local-password <логин> или --delete-local-character <логин> <имя>");
            return 1;
        }

        Console.WriteLine("=== Dragon Knight Server ===");
        Console.WriteLine("Запуск сервера...");

        TcpListener server = new TcpListener(IPAddress.Loopback, 3800);
        try
        {
            server.Start();
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            Console.Error.WriteLine("Порт 127.0.0.1:3800 уже занят. Закройте предыдущий экземпляр сервера.");
            return 1;
        }

        Console.WriteLine($"Сервер запущен на порту 3800");
        Console.WriteLine("Ожидание подключений...");

        while (true)
        {
            try
            {
                var tcp = await server.AcceptTcpClientAsync();
                Console.WriteLine("Новое подключение!");

                var client = new ClientSession(tcp);
                _ = client.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }
}
