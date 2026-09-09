using System.Net;
using System.Net.Sockets;

namespace DragonKnightServer;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== Dragon Knight Server ===");
        Console.WriteLine("Запуск сервера...");

        TcpListener server = new TcpListener(IPAddress.Any, 3800);
        server.Start();

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
