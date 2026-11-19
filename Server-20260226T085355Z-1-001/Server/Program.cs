using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DragonKnightServer
{
    class Program
    {
        private const int AUTH_PORT = 3791;
        private const int GAME_PORT = 3800;

        static async Task Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("🐉 Dragon Knight Online Server (с учетом APPPROC.lua)");
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
                Console.WriteLine($"[Server] Клиент подключился: {client.Client.RemoteEndPoint}");
                _ = Task.Run(() => GameServerHandler.HandleClientAsync(client));
            }
        }
    }

    internal static class GameServerHandler
    {
        private static readonly List<byte> _in = new(8192);
        private static bool _initialPacketsSent = false;
        private static int _clientId = 0;

        // Opcode
        private const ushort OP_HEARTBEAT = 0x0200;
        private const ushort OP_LOGIN = 0x0064;
        private const ushort OP_SERVER_LIST = 0x0065;
        private const ushort OP_ACCOUNT_SAVE = 0x0066;
        private const ushort OP_LOGIN_SUCCESS = 0x000B;
        private const ushort OP_CHAR_LIST = 0x001A;
        private const ushort OP_CREATE_INFO = 0x0069;
        private const ushort OP_CREATE_CHAR = 0x006A;
        private const ushort OP_CREATE_CHAR_SUCCESS = 0x006B;
        private const ushort OP_EXP_CHANGE = 0x006C;
        private const ushort OP_HARM = 0x006D;
        private const ushort OP_GOODS_HINT = 0x006E;
        private const ushort OP_ENTER_WORLD = 0x0017;
        private const ushort OP_WORLD_INFO = 0x0018;
        private const ushort OP_PLAYER_STATUS = 0x0019;
        private const ushort OP_INVENTORY = 0x001C;
        private const ushort OP_TIME_SYNC = 0x001D;
        private const ushort OP_PLAYER_LIST = 0x001E;
        private const ushort OP_NPC_LIST = 0x001F;
        private const ushort OP_MOVE = 0x0020;
        private const ushort OP_STATUS_UPDATE = 0x0021;
        private const ushort OP_STATUS_REQUEST = 0x0012;
        private const ushort OP_SKILL_LIST = 0x0013;
        private const ushort OP_QUEST_LIST = 0x0014;
        private const ushort OP_ACHIEVEMENTS = 0x0015;
        private const ushort OP_SPAWN_NPC = 0x0016;
        private const ushort OP_HANDSHAKE = 0x0012;

        public static async Task HandleClientAsync(TcpClient tcp)
        {
            var ep = tcp.Client.RemoteEndPoint?.ToString() ?? "?";
            int currentClientId = ++_clientId;
            Console.WriteLine($"[Server:{currentClientId}] Обработка клиента: {ep}");

            tcp.NoDelay = true;
            tcp.ReceiveBufferSize = 8192;
            tcp.SendBufferSize = 8192;

            NetworkStream s = tcp.GetStream();

            try
            {
                var buf = new byte[8192];
                _initialPacketsSent = false;
                bool handshakeDone = false;

                while (tcp.Connected)
                {
                    int r = 0;
                    try
                    {
                        r = await s.ReadAsync(buf, 0, buf.Length);
                    }
                    catch
                    {
                        break;
                    }

                    if (r <= 0) break;

                    for (int i = 0; i < r; i++) _in.Add(buf[i]);

                    while (_in.Count >= 4 && _in[0] == 0x00 && _in[1] == 0x02 && _in[2] == 0x00 && _in[3] == 0x02)
                    {
                        _in.RemoveRange(0, 4);

                        byte[] heartbeatResponse = { 0x02, 0x00, 0x00, 0x02 };
                        await s.WriteAsync(heartbeatResponse, 0, heartbeatResponse.Length);
                        await s.FlushAsync();
                        Console.WriteLine($"[Server:{currentClientId}] >>> Heartbeat ответ");

                        if (!handshakeDone)
                        {
                            // ДОБАВЛЯЕМ ЗАДЕРЖКУ ПЕРЕД HANDSHAKE!
                            await Task.Delay(100);
                            await SendHandshake(s, currentClientId);
                            handshakeDone = true;
                        }
                    }

                    while (TryPopFrame(out var frame, currentClientId))
                    {
                        if (frame.Length < 4) continue;

                        ushort opcode = (ushort)((frame[2] << 8) | frame[3]);
                        Console.WriteLine($"[Server:{currentClientId}] <<< Пакет opcode=0x{opcode:X4}, размер={frame.Length}");

                        if (opcode == OP_HANDSHAKE && frame.Length == 73)
                        {
                            await HandleHandshakeResponse(s, frame, currentClientId);
                            continue;
                        }

                        switch (opcode)
                        {
                            case OP_CHAR_LIST:
                                await HandleCharListRequest(s, frame, currentClientId);
                                break;

                            case OP_LOGIN:
                                await HandleLogin(s, frame, currentClientId);
                                break;

                            case OP_CREATE_CHAR:
                                await HandleCreateChar(s, frame, currentClientId);
                                break;

                            case OP_ENTER_WORLD:
                                await HandleEnterWorld(s, frame, currentClientId);
                                break;

                            case OP_STATUS_REQUEST:
                                await HandleStatusRequest(s, frame, currentClientId);
                                break;

                            case OP_SKILL_LIST:
                                await HandleSkillListRequest(s, frame, currentClientId);
                                break;

                            default:
                                Console.WriteLine($"[Server:{currentClientId}] Неизвестный opcode: 0x{opcode:X4}");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server:{currentClientId}] Ошибка: {ex.Message}");
            }
            finally
            {
                _in.Clear();
                _initialPacketsSent = false;
                tcp.Close();
                Console.WriteLine($"[Server:{currentClientId}] Отключен {ep}");
            }
        }

        private static async Task SendHandshake(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка Handshake (31 байт)");

            // ТОЧНАЯ КОПИЯ из Wireshark (31 байт, БЕЗ заголовка!)
            byte[] handshakeData = new byte[]
            {
                0x00, 0x00, 0x00, 0x1f, 0x00, 0x00, 0x00, 0x00,
                0x04, 0x03, 0x00, 0x11, 0x67, 0x6a, 0x36, 0x42,
                0x4b, 0x68, 0x4d, 0x4c, 0x4e, 0x4d, 0x71, 0x6e,
                0x49, 0x6a, 0x6b, 0x41, 0x00, 0x00, 0x00
            };

            await s.WriteAsync(handshakeData, 0, handshakeData.Length);
            await s.FlushAsync();

            Console.WriteLine($"[Server:{clientId}] >>> Отправлен Handshake, {handshakeData.Length} байт");
        }

        private static async Task HandleHandshakeResponse(NetworkStream s, byte[] frame, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] <<< Handshake ответ принят (69 байт)");

            await Task.Delay(100);
            await SendServerList(s, clientId);
            await Task.Delay(100);
            await SendAccountSave(s, clientId);
            await Task.Delay(100);
            await SendLoginSuccess(s, clientId);
            await Task.Delay(100);
            await SendCharList(s, clientId);
            await Task.Delay(100);
            await SendCreateInfo(s, clientId);
        }

        private static async Task HandleStatusRequest(NetworkStream s, byte[] frame, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] <<< Запрос статуса (0x0012)");
            await SendPlayerStatus(s, clientId);
        }

        private static async Task HandleSkillListRequest(NetworkStream s, byte[] frame, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] <<< Запрос списка умений (0x0013)");
            await SendSkillList(s, clientId);
        }

        private static async Task HandleCharListRequest(NetworkStream s, byte[] frame, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] <<< Запрос списка персонажей");
            await SendCharList(s, clientId);
        }

        private static async Task HandleLogin(NetworkStream s, byte[] frame, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] Обработка логина...");

            try
            {
                int offset = 4;

                byte loginMode = frame[offset++];

                int nameEnd = offset;
                while (nameEnd < frame.Length && frame[nameEnd] != 0) nameEnd++;
                string account = Encoding.UTF8.GetString(frame, offset, nameEnd - offset);
                offset = nameEnd + 1;

                int passEnd = offset;
                while (passEnd < frame.Length && frame[passEnd] != 0) passEnd++;
                string password = Encoding.UTF8.GetString(frame, offset, passEnd - offset);

                Console.WriteLine($"[Server:{clientId}] Логин: аккаунт='{account}', пароль='{password}'");

                await Task.Delay(100);
                await SendCharList(s, clientId);
                await Task.Delay(100);
                await SendCreateInfo(s, clientId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server:{clientId}] Ошибка логина: {ex.Message}");
            }
        }

        private static async Task HandleCreateChar(NetworkStream s, byte[] frame, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] Обработка создания персонажа...");

            if (frame.Length < 20)
            {
                await Send(s, OP_CREATE_CHAR_SUCCESS, new byte[] { 0x00 }, clientId);
                return;
            }

            try
            {
                int offset = 4;

                int nameEnd = offset;
                while (nameEnd < frame.Length && frame[nameEnd] != 0) nameEnd++;
                string name = Encoding.UTF8.GetString(frame, offset, nameEnd - offset);
                offset = nameEnd + 1;

                if (offset + 12 > frame.Length)
                {
                    await Send(s, OP_CREATE_CHAR_SUCCESS, new byte[] { 0x00 }, clientId);
                    return;
                }

                byte jobId = frame[offset++];
                byte gender = frame[offset++];
                int hairId = BitConverter.ToInt32(frame, offset); offset += 4;
                int faceId = BitConverter.ToInt32(frame, offset); offset += 4;
                int hairColor = BitConverter.ToInt32(frame, offset);

                Console.WriteLine($"[Server:{clientId}] Создание: имя='{name}', job={jobId}, пол={gender}");

                await Send(s, OP_CREATE_CHAR_SUCCESS, new byte[] { 0x01 }, clientId);

                await Task.Delay(100);
                await SendCharList(s, clientId);
                await Task.Delay(100);
                await SendCreateInfo(s, clientId);

                await Task.Delay(100);
                await SendExpChange(s, clientId, 100);
                await Task.Delay(100);
                await SendHarm(s, clientId, 50);
            }
            catch
            {
                await Send(s, OP_CREATE_CHAR_SUCCESS, new byte[] { 0x00 }, clientId);
            }
        }

        private static async Task HandleEnterWorld(NetworkStream s, byte[] frame, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] <<< Запрос входа в мир");
            await SendAllWorldPackets(s, clientId);
        }

        private static void WriteNullTerminatedString(BinaryWriter writer, string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            writer.Write(bytes);
            writer.Write((byte)0);
        }

        private static async Task SendServerList(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка списка серверов (MSG_APP_REPLY_SERVER_LIST)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(1);
            WriteNullTerminatedString(writer, "Тестовый сервер");
            writer.Write(2);
            WriteNullTerminatedString(writer, "Подсервер 1");
            writer.Write(1);
            WriteNullTerminatedString(writer, "127.0.0.1");
            WriteNullTerminatedString(writer, "Подсервер 2");
            writer.Write(1);
            WriteNullTerminatedString(writer, "127.0.0.1");

            byte[] payload = ms.ToArray();
            await Send(s, OP_SERVER_LIST, payload, clientId);
        }

        private static async Task SendAccountSave(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка сохраненного аккаунта (MSG_APP_ACCOUNT_SAVE)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write((byte)1);
            WriteNullTerminatedString(writer, "test");
            writer.Write(0);
            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_ACCOUNT_SAVE, payload, clientId);
        }

        private static async Task SendLoginSuccess(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка LoginSuccess (0x000B)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write((byte)1);
            writer.Write(1001);
            writer.Write((byte)1);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write(0);
            WriteNullTerminatedString(writer, "test");
            writer.Write(0);
            writer.Write(0);
            writer.Write((byte)0);
            writer.Write(0);
            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_LOGIN_SUCCESS, payload, clientId);
        }

        private static async Task SendCharList(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка CharList (0x001A)");

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write((byte)1);

            writer.Write(1);
            writer.Write((byte)1);

            string name = "Тест";
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            writer.Write(nameBytes);
            for (int i = nameBytes.Length; i < 16; i++)
                writer.Write((byte)0);

            writer.Write((byte)1);
            writer.Write((byte)1);
            writer.Write((byte)0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            writer.Write(1);
            writer.Write(1);
            writer.Write(0);

            writer.Write(0);
            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_CHAR_LIST, payload, clientId);
        }

        private static async Task SendCreateInfo(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка CreateInfo (0x0069)");

            byte classCount = 10;
            var payload = new byte[1 + classCount * 2];
            payload[0] = classCount;

            int off = 1;
            for (ushort i = 1; i <= classCount; i++)
            {
                payload[off++] = (byte)(i & 0xFF);
                payload[off++] = (byte)(i >> 8);
            }

            await Send(s, OP_CREATE_INFO, payload, clientId);
        }

        private static async Task SendAllWorldPackets(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка всех пакетов мира...");

            await SendEnterWorld(s, clientId);
            await Task.Delay(50);

            await SendWorldInfo(s, clientId);
            await Task.Delay(50);

            await SendPlayerStatus(s, clientId);
            await Task.Delay(50);

            await SendInventory(s, clientId);
            await Task.Delay(50);

            await SendTimeSync(s, clientId);
            await Task.Delay(50);

            await SendPlayerList(s, clientId);
            await Task.Delay(50);

            await SendNpcList(s, clientId);
            await Task.Delay(50);

            await SendMove(s, clientId);
            await Task.Delay(50);

            await SendExpChange(s, clientId, 0);
            await Task.Delay(50);

            await SendHarm(s, clientId, 0);
            await Task.Delay(50);

            await SendSkillList(s, clientId);
            await Task.Delay(50);

            await SendQuestList(s, clientId);
            await Task.Delay(50);

            await SendAchievements(s, clientId);
            await Task.Delay(50);

            await SendSpawnNpc(s, clientId);

            Console.WriteLine($"[Server:{clientId}] >>> Все пакеты мира отправлены");
        }

        private static async Task SendEnterWorld(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка EnterWorld (0x0017)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write((byte)1);
            writer.Write(1);
            writer.Write(1);
            writer.Write(100.0f);
            writer.Write(200.0f);
            writer.Write(0.0f);
            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_ENTER_WORLD, payload, clientId);
        }

        private static async Task SendWorldInfo(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка WorldInfo (0x0018)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(1);
            writer.Write(100);
            writer.Write(100);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_WORLD_INFO, payload, clientId);
        }

        private static async Task SendPlayerStatus(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка PlayerStatus (0x0019)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(1);
            writer.Write(100);
            writer.Write(100);
            writer.Write(50);
            writer.Write(50);
            writer.Write(1);
            writer.Write(0);
            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_PLAYER_STATUS, payload, clientId);
        }

        private static async Task SendSkillList(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка SkillList (0x0013)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_SKILL_LIST, payload, clientId);
        }

        private static async Task SendQuestList(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка QuestList (0x0014)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_QUEST_LIST, payload, clientId);
        }

        private static async Task SendAchievements(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка Achievements (0x0015)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_ACHIEVEMENTS, payload, clientId);
        }

        private static async Task SendSpawnNpc(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка SpawnNpc (0x0016)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_SPAWN_NPC, payload, clientId);
        }

        private static async Task SendInventory(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка Inventory (0x001C)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_INVENTORY, payload, clientId);
        }

        private static async Task SendTimeSync(NetworkStream s, int clientId)
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_TIME_SYNC, payload, clientId);
        }

        private static async Task SendPlayerList(NetworkStream s, int clientId)
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_PLAYER_LIST, payload, clientId);
        }

        private static async Task SendNpcList(NetworkStream s, int clientId)
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_NPC_LIST, payload, clientId);
        }

        private static async Task SendMove(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка Move (0x0020)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(1);
            writer.Write(100.0f);
            writer.Write(200.0f);
            writer.Write(0.0f);
            writer.Write(0);
            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_MOVE, payload, clientId);
        }

        private static async Task SendStatusUpdate(NetworkStream s, int clientId)
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(1);
            writer.Write(100);
            writer.Write(100);
            writer.Write(50);
            writer.Write(50);

            byte[] payload = ms.ToArray();
            await Send(s, OP_STATUS_UPDATE, payload, clientId);
        }

        private static async Task SendExpChange(NetworkStream s, int clientId, int exp)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка изменения опыта (MSG_APP_EXP_CHANGE)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(0);
            writer.Write(exp);

            byte[] payload = ms.ToArray();
            await Send(s, OP_EXP_CHANGE, payload, clientId);
        }

        private static async Task SendHarm(NetworkStream s, int clientId, int damage)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка урона (MSG_APP_HARM)");

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(0);
            writer.Write(1);
            writer.Write(damage);
            writer.Write((byte)0);
            writer.Write(0);

            byte[] payload = ms.ToArray();
            await Send(s, OP_HARM, payload, clientId);
        }

        private static async Task Send(NetworkStream s, ushort opcode, byte[] payload, int clientId)
        {
            try
            {
                ushort size = (ushort)(4 + payload.Length);
                var packet = new byte[size];

                packet[0] = (byte)(size >> 8);
                packet[1] = (byte)(size & 0xFF);
                packet[2] = (byte)(opcode >> 8);
                packet[3] = (byte)(opcode & 0xFF);

                if (payload.Length > 0)
                    Buffer.BlockCopy(payload, 0, packet, 4, payload.Length);

                await s.WriteAsync(packet, 0, packet.Length);
                await s.FlushAsync();

                Console.WriteLine($"[Server:{clientId}] >>> Отправлен opcode=0x{opcode:X4}, {packet.Length} байт");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server:{clientId}] Ошибка отправки: {ex.Message}");
                throw;
            }
        }

        private static bool TryPopFrame(out byte[] frame, int clientId)
        {
            frame = Array.Empty<byte>();
            if (_in.Count < 4) return false;

            ushort size = (ushort)((_in[0] << 8) | _in[1]);

            if (size < 4 || size > 8192)
            {
                _in.RemoveAt(0);
                return false;
            }

            if (_in.Count < size) return false;

            frame = _in.GetRange(0, size).ToArray();
            _in.RemoveRange(0, size);
            return true;
        }
    }
}