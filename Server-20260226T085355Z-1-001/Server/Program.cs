using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DragonDualPort
{
    class Program
    {
        private const int AUTH_PORT = 3791;
        private const int GAME_PORT = 3800;

        static async Task Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine($"🐉 Dragon Knight Online — AUTH ({AUTH_PORT}) + GameServer ({GAME_PORT}) 🏆🔥");
            var authTask = StartAuthServer();
            var gameTask = StartGameServerStub();
            await Task.WhenAll(authTask, gameTask);
        }

        private static async Task StartAuthServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, AUTH_PORT);
            listener.Start();
            Console.WriteLine($"[AUTH] Слушаю порт {AUTH_PORT}");
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => AuthHandler.HandleClientAsync(client));
            }
        }

        private static async Task StartGameServerStub()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, GAME_PORT);
            listener.Start();
            Console.WriteLine($"[GameServer] Заглушка слушает порт {GAME_PORT}");
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine($"[GameServer] Клиент подключился к 3800: {client.Client.RemoteEndPoint}");
                _ = Task.Run(() => GameStubHandler.HandleClientAsync(client));
            }
        }
    }

    internal static class AuthHandler
    {
        private static readonly List<byte> _in = new(8192);
        private static bool _sentPackets = false;

        private const ushort OP_LOGIN_SUCCESS = 0x000B;
        private const ushort OP_CREATE_INFO = 0x0069;
        private const ushort OP_CREATE_CHAR = 0x006A;
        private const ushort OP_CREATE_CHAR_SUCCESS = 0x006B;
        private const ushort OP_CHAR_LIST = 0x001A; // Правильный opcode

        // Валидные ID из Lua
        private static readonly Dictionary<int, HashSet<int>> ValidHairIds = new()
        {
            {1, new HashSet<int>{42017,42018,42022,42088,42039}},
            {6, new HashSet<int>{42078,42045,42047,42081,42090}},
            {2, new HashSet<int>{42013,42014,42024,42086,42035}},
            {7, new HashSet<int>{42076,42049,42051,42053,42084}},
            {8, new HashSet<int>{42080,42061,42063,42065,42098}},
            {3, new HashSet<int>{42009,42010,42026,42096,42033}},
            {9, new HashSet<int>{42043,42067,42069,42071,42094}},
            {4, new HashSet<int>{42000,42001,42030,42092,42031}},
            {10, new HashSet<int>{42074,42055,42057,42059,42102}},
            {5, new HashSet<int>{42005,42006,42028,42100,42037}}
        };

        private static readonly Dictionary<int, HashSet<int>> ValidFaceIds = new()
        {
            {1, new HashSet<int>{42019,42020,42021,42087,42040}},
            {6, new HashSet<int>{42077,42046,42048,42082,42089}},
            {2, new HashSet<int>{42015,42016,42023,42085,42036}},
            {7, new HashSet<int>{42075,42050,42052,42054,42083}},
            {8, new HashSet<int>{42079,42062,42064,42066,42097}},
            {3, new HashSet<int>{42011,42012,42025,42095,42034}},
            {9, new HashSet<int>{42044,42068,42070,42072,42093}},
            {4, new HashSet<int>{42003,42004,42029,42091,42032}},
            {10, new HashSet<int>{42073,42056,42058,42060,42101}},
            {5, new HashSet<int>{42007,42008,42027,42099,42038}}
        };

        public static async Task HandleClientAsync(TcpClient tcp)
        {
            var ep = tcp.Client.RemoteEndPoint?.ToString() ?? "?";
            Console.WriteLine($"[AUTH] Клиент подключился: {ep}");
            tcp.NoDelay = true;
            NetworkStream s = tcp.GetStream();

            try
            {
                var buf = new byte[8192];
                while (tcp.Connected)
                {
                    int r = await s.ReadAsync(buf, 0, buf.Length);
                    if (r <= 0) break;

                    for (int i = 0; i < r; i++) _in.Add(buf[i]);

                    // Heartbeat
                    while (_in.Count >= 4 && _in[0] == 0x00 && _in[1] == 0x02 && _in[2] == 0x00 && _in[3] == 0x02)
                    {
                        _in.RemoveRange(0, 4);
                        await s.WriteAsync(new byte[] { 0x02, 0x00, 0x00, 0x02 });
                        Console.WriteLine("[AUTH] >>> Ответ на heartbeat");

                        if (!_sentPackets)
                        {
                            await SendLoginSuccess(s);
                            await SendCreateInfo(s);
                            await SendCharList(s);
                            _sentPackets = true;
                        }
                    }

                    // Пакеты
                    while (TryPopFrame(out var frame))
                    {
                        ushort opcode = (ushort)((frame[2] << 8) | frame[3]);
                        Console.WriteLine($"[AUTH] <<< Пакет opcode=0x{opcode:X4}");

                        if (opcode == OP_CREATE_CHAR)
                        {
                            await HandleCreateChar(s, frame);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTH] Ошибка: {ex.Message}");
            }
            finally
            {
                tcp.Close();
                Console.WriteLine($"[AUTH] Disconnect {ep}");
            }
        }

        private static async Task HandleCreateChar(NetworkStream s, byte[] frame)
        {
            if (frame.Length < 20)
            {
                await Send(s, OP_CREATE_CHAR_SUCCESS, new byte[] { 0x00 });
                return;
            }

            int offset = 4;
            int nameEnd = Array.IndexOf(frame, (byte)0, offset);
            if (nameEnd == -1) nameEnd = frame.Length;
            string name = Encoding.UTF8.GetString(frame, offset, nameEnd - offset);
            offset = nameEnd + 1;

            if (offset + 12 > frame.Length)
            {
                await Send(s, OP_CREATE_CHAR_SUCCESS, new byte[] { 0x00 });
                return;
            }

            byte jobId = frame[offset++];
            byte gender = frame[offset++];
            int hairId = BitConverter.ToInt32(frame, offset); offset += 4;
            int faceId = BitConverter.ToInt32(frame, offset); offset += 4;
            int hairColor = BitConverter.ToInt32(frame, offset);

            Console.WriteLine($"[AUTH] Создание: имя='{name}', job={jobId}, gender={gender}, hair={hairId}, face={faceId}, color=0x{hairColor:X8}");

            int classKey = jobId;
            if (!ValidHairIds.TryGetValue(classKey, out var hairs) || !hairs.Contains(hairId) ||
                !ValidFaceIds.TryGetValue(classKey, out var faces) || !faces.Contains(faceId))
            {
                Console.WriteLine("[AUTH] Invalid hair/face — fail");
                await Send(s, OP_CREATE_CHAR_SUCCESS, new byte[] { 0x00 });
                return;
            }

            // Здесь сохрани в БД
            Console.WriteLine("[AUTH] Чар создан!");
            await Send(s, OP_CREATE_CHAR_SUCCESS, new byte[] { 0x01 });
        }

        private static async Task SendCharList(NetworkStream s)
        {
            Console.WriteLine("[AUTH] >>> Шлём char list (0x001A)");
            var payload = new byte[68]; // 68 байт для пустого списка
            payload[0] = 0x00; // count = 0
            await Send(s, OP_CHAR_LIST, payload);
        }

        private static async Task SendLoginSuccess(NetworkStream s)
        {
            Console.WriteLine("[AUTH] >>> Login success (0x000B)");
            var payload = new byte[772];
            payload[0x28] = 0x01;
            payload[0x29] = 0x00;
            await Send(s, OP_LOGIN_SUCCESS, payload);
        }

        private static async Task SendCreateInfo(NetworkStream s)
        {
            Console.WriteLine("[AUTH] >>> Create info (0x0069)");
            byte classCount = 10;
            var payload = new byte[1 + classCount * 2];
            payload[0] = classCount;
            int off = 1;
            for (ushort i = 1; i <= classCount; i++)
            {
                payload[off++] = (byte)(i & 0xFF);
                payload[off++] = (byte)(i >> 8);
            }
            await Send(s, OP_CREATE_INFO, payload);
        }

        private static async Task Send(NetworkStream s, ushort opcode, byte[] payload)
        {
            byte[] packet = Pkt.BuildBE(opcode, payload);
            await s.WriteAsync(packet);
            await s.FlushAsync();
            Console.WriteLine($"[AUTH] >>> Отправлено opcode=0x{opcode:X4}, {packet.Length} байт");
        }

        private static bool TryPopFrame(out byte[] frame)
        {
            frame = Array.Empty<byte>();
            if (_in.Count < 4) return false;
            ushort size = (ushort)((_in[0] << 8) | _in[1]);
            if (size < 4 || size > 8192) { _in.RemoveAt(0); return false; }
            if (_in.Count < size) return false;
            frame = _in.GetRange(0, size).ToArray();
            _in.RemoveRange(0, size);
            return true;
        }
    }

    internal static class GameStubHandler
    {
        public static async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[8192];
                while (client.Connected)
                {
                    int bytes = await stream.ReadAsync(buffer);
                    if (bytes == 0) break;
                    Console.WriteLine($"[GameServer] Получено {bytes} байт на 3800");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameServer] Ошибка: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("[GameServer] Клиент отключился от 3800");
            }
        }
    }

    internal static class Pkt
    {
        public static byte[] BuildBE(ushort opcode, byte[] payload)
        {
            ushort size = (ushort)(4 + payload.Length);
            var p = new byte[size];
            p[0] = (byte)(size >> 8);
            p[1] = (byte)(size & 0xFF);
            p[2] = (byte)(opcode >> 8);
            p[3] = (byte)(opcode & 0xFF);
            if (payload.Length > 0) Buffer.BlockCopy(payload, 0, p, 4, payload.Length);
            return p;
        }
    }
}