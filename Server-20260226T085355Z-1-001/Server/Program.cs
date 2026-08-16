using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace DragonKnightServer
{
    class Program
    {
        private const int GAME_PORT = 3800;

        static async Task Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("🐉 Dragon Knight Online Server");
            await StartGameServer();
        }

        private static async Task StartGameServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, GAME_PORT);
            listener.Start();
            Console.WriteLine($"[Server] Запущен на порту {GAME_PORT}");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine($"[Server] Клиент подключился");
                _ = Task.Run(() => GameServerHandler.HandleClientAsync(client));
            }
        }
    }

    internal static class GameServerHandler
    {
        private const ushort OP_HEARTBEAT = 0x0200;
        private const ushort OP_LOGIN = 0x0064;
        private const ushort OP_SERVER_LIST = 0x0065;
        private const ushort OP_ACCOUNT_SAVE = 0x0066;
        private const ushort OP_LOGIN_SUCCESS = 0x000B;
        private const ushort OP_CHAR_LIST = 0x001A;
        private const ushort OP_CREATE_INFO = 0x0069;
        private const ushort OP_CREATE_CHAR = 0x006A;
        private const ushort OP_ENTER_WORLD = 0x0017;

        private static bool _initialized = false;

        public static async Task HandleClientAsync(TcpClient tcp)
        {
            Console.WriteLine($"[Server] Начало обработки клиента");
            _initialized = false;

            try
            {
                tcp.NoDelay = true;
                using (NetworkStream stream = tcp.GetStream())
                {
                    byte[] buffer = new byte[8192];

                    while (tcp.Connected)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;

                        Console.WriteLine($"[Server] Получено {bytesRead} байт");
                        string hex = BitConverter.ToString(buffer, 0, bytesRead).Replace("-", " ");
                        Console.WriteLine($"[Server] HEX: {hex}");

                        if (bytesRead >= 2)
                        {
                            ushort opcode = (ushort)(buffer[0] | (buffer[1] << 8));
                            Console.WriteLine($"[Server] Opcode: 0x{opcode:X4}");

                            switch (opcode)
                            {
                                case OP_HEARTBEAT:
                                    Console.WriteLine("[Server] Heartbeat получен!");
                                    byte[] response = { 0x00, 0x02 };
                                    await stream.WriteAsync(response, 0, response.Length);
                                    await stream.FlushAsync();
                                    Console.WriteLine($"[Server] >>> Heartbeat ответ: {BitConverter.ToString(response).Replace("-", " ")}");

                                    if (!_initialized)
                                    {
                                        await SendServerList(stream);
                                        await Task.Delay(50);
                                        await SendAccountSave(stream);
                                        await Task.Delay(50);

                                        // Отправляем LoginSuccess ДО того, как клиент запросит
                                        await SendLoginSuccess(stream);

                                        _initialized = true;
                                    }
                                    break;

                                case OP_LOGIN:
                                    Console.WriteLine("[Server] Login запрос получен!");
                                    await SendLoginSuccess(stream);
                                    break;

                                case OP_CHAR_LIST:
                                    Console.WriteLine("[Server] CharList запрос получен!");
                                    // ТЕПЕРЬ отправляем CharList в ответ на запрос!
                                    await SendCharList(stream);
                                    await Task.Delay(50);
                                    await SendCreateInfo(stream);
                                    break;

                                case OP_CREATE_CHAR:
                                    Console.WriteLine("[Server] CreateChar запрос получен!");
                                    await SendCreateCharSuccess(stream);
                                    break;

                                case OP_ENTER_WORLD:
                                    Console.WriteLine("[Server] EnterWorld запрос получен!");
                                    await SendEnterWorld(stream);
                                    break;

                                default:
                                    Console.WriteLine($"[Server] Неизвестный opcode: 0x{opcode:X4}");
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Ошибка: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("[Server] Клиент отключен");
            }
        }

        private static async Task SendServerList(NetworkStream stream)
        {
            Console.WriteLine("[Server] >>> Отправка ServerList (0x0065)");
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.Write((byte)1);
            WriteString(writer, "Test Server");
            writer.Write((byte)1);
            WriteString(writer, "Game Server");
            writer.Write((byte)1);
            WriteString(writer, "127.0.0.1");
            await SendPacket(stream, OP_SERVER_LIST, ms.ToArray());
        }

        private static async Task SendAccountSave(NetworkStream stream)
        {
            Console.WriteLine("[Server] >>> Отправка AccountSave (0x0066)");
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.Write((byte)1);
            WriteString(writer, "test");
            writer.Write(0);
            writer.Write(0);
            await SendPacket(stream, OP_ACCOUNT_SAVE, ms.ToArray());
        }

        private static async Task SendLoginSuccess(NetworkStream stream)
        {
            Console.WriteLine("[Server] >>> Отправка LoginSuccess (0x000B) - С ИМЕНЕМ");

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.Write((byte)1);  // Успех
            writer.Write(1001);     // ID аккаунта
            WriteString(writer, "test");  // Имя
            await SendPacket(stream, OP_LOGIN_SUCCESS, ms.ToArray());
        }

        private static async Task SendCharList(NetworkStream stream)
        {
            Console.WriteLine("[Server] >>> Отправка CharList (0x001A) - В ОТВЕТ НА ЗАПРОС");
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.Write((byte)1);  // 1 персонаж
            writer.Write(1);        // ID
            string name = "TestChar";
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            writer.Write(nameBytes);
            writer.Write((byte)0);
            for (int i = nameBytes.Length + 1; i < 16; i++)
                writer.Write((byte)0);
            writer.Write(1);  // Уровень
            writer.Write(1);  // Класс
            writer.Write(0);  // Пол
            byte[] data = ms.ToArray();
            if (data.Length != 33) Array.Resize(ref data, 33);
            await SendPacket(stream, OP_CHAR_LIST, data);
        }

        private static async Task SendCreateInfo(NetworkStream stream)
        {
            Console.WriteLine("[Server] >>> Отправка CreateInfo (0x0069)");
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            byte classCount = 10;
            writer.Write(classCount);
            for (ushort i = 1; i <= classCount; i++)
                writer.Write(i);
            await SendPacket(stream, OP_CREATE_INFO, ms.ToArray());
        }

        private static async Task SendCreateCharSuccess(NetworkStream stream)
        {
            Console.WriteLine("[Server] >>> Отправка CreateCharSuccess (0x006B)");
            await SendPacket(stream, 0x006B, new byte[] { 0x01 });
            await Task.Delay(50);
            await SendCharList(stream);
        }

        private static async Task SendEnterWorld(NetworkStream stream)
        {
            Console.WriteLine("[Server] >>> Отправка EnterWorld (0x0017)");
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.Write((byte)1);
            writer.Write(1);
            writer.Write(1);
            writer.Write(100.0f);
            writer.Write(200.0f);
            writer.Write(0.0f);
            writer.Write(0);
            await SendPacket(stream, OP_ENTER_WORLD, ms.ToArray());
        }

        private static async Task SendPacket(NetworkStream stream, ushort opcode, byte[] payload)
        {
            byte[] packet = new byte[2 + payload.Length];
            packet[0] = (byte)(opcode >> 8);
            packet[1] = (byte)(opcode & 0xFF);
            if (payload.Length > 0)
                Buffer.BlockCopy(payload, 0, packet, 2, payload.Length);

            Console.WriteLine($"[Server] >>> Отправлен opcode=0x{opcode:X4}, {packet.Length} байт");
            Console.WriteLine($"[Server] HEX: {BitConverter.ToString(packet).Replace("-", " ")}");

            await stream.WriteAsync(packet, 0, packet.Length);
            await stream.FlushAsync();
        }

        private static void WriteString(BinaryWriter writer, string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            writer.Write(bytes);
            writer.Write((byte)0);
        }
    }
}