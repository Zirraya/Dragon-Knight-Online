﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DragonKnightServer
{
    class Program
    {
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

        // Opcode из APPPROC.lua и анализа
        private const ushort OP_HEARTBEAT = 0x0200; // Heartbeat пакет
        private const ushort OP_LOGIN = 0x0064;     // MSG_APP_LOGIN (предположительно)
        private const ushort OP_SERVER_LIST = 0x0065; // MSG_APP_REPLY_SERVER_LIST
        private const ushort OP_ACCOUNT_SAVE = 0x0066; // MSG_APP_ACCOUNT_SAVE
        private const ushort OP_LOGIN_SUCCESS = 0x000B;
        private const ushort OP_CHAR_LIST = 0x001A;
        private const ushort OP_CREATE_INFO = 0x0069;
        private const ushort OP_CREATE_CHAR = 0x006A;
        private const ushort OP_CREATE_CHAR_SUCCESS = 0x006B;
        private const ushort OP_EXP_CHANGE = 0x006C; // MSG_APP_EXP_CHANGE
        private const ushort OP_HARM = 0x006D;       // MSG_APP_HARM
        private const ushort OP_GOODS_HINT = 0x006E; // MSG_APP_GOODS_HINT

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

                    // Обработка heartbeat (как в APPPROC.lua)
                    while (_in.Count >= 4 && _in[0] == 0x00 && _in[1] == 0x02 && _in[2] == 0x00 && _in[3] == 0x02)
                    {
                        _in.RemoveRange(0, 4);
                        
                        byte[] heartbeatResponse = { 0x02, 0x00, 0x00, 0x02 };
                        await s.WriteAsync(heartbeatResponse, 0, heartbeatResponse.Length);
                        await s.FlushAsync();
                        Console.WriteLine($"[Server:{currentClientId}] >>> Heartbeat ответ");

                        // Отправляем начальные пакеты только один раз
                        if (!_initialPacketsSent)
                        {
                            Console.WriteLine($"[Server:{currentClientId}] Отправка начальных пакетов...");
                            
                            // Сначала отправляем список серверов как в MSG_APP_REPLY_SERVER_LIST
                            await SendServerList(s, currentClientId);
                            await Task.Delay(50);
                            
                            // Затем загружаем сохраненный аккаунт
                            await SendAccountSave(s, currentClientId);
                            await Task.Delay(50);
                            
                            // Отправляем успешный логин
                            await SendLoginSuccess(s, currentClientId);
                            
                            _initialPacketsSent = true;
                            Console.WriteLine($"[Server:{currentClientId}] Начальные пакеты отправлены");
                        }
                    }

                    // Обработка входящих пакетов
                    while (TryPopFrame(out var frame, currentClientId))
                    {
                        if (frame.Length < 4) continue;
                        
                        ushort opcode = (ushort)((frame[2] << 8) | frame[3]);
                        Console.WriteLine($"[Server:{currentClientId}] <<< Пакет opcode=0x{opcode:X4}, размер={frame.Length}");

                        switch (opcode)
                        {
                            case OP_LOGIN:
                                await HandleLogin(s, frame, currentClientId);
                                break;
                                
                            case OP_CREATE_CHAR:
                                await HandleCreateChar(s, frame, currentClientId);
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

        private static async Task HandleLogin(NetworkStream s, byte[] frame, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] Обработка логина...");
            
            try
            {
                // Парсим как в APPPROC.lua с SKYParamPop
                int offset = 4;
                
                // Режим логина (1 для стандартного)
                byte loginMode = frame[offset++];
                
                // Читаем имя аккаунта (null-terminated)
                int nameEnd = offset;
                while (nameEnd < frame.Length && frame[nameEnd] != 0) nameEnd++;
                string account = Encoding.UTF8.GetString(frame, offset, nameEnd - offset);
                offset = nameEnd + 1;
                
                // Читаем пароль
                int passEnd = offset;
                while (passEnd < frame.Length && frame[passEnd] != 0) passEnd++;
                string password = Encoding.UTF8.GetString(frame, offset, passEnd - offset);
                
                Console.WriteLine($"[Server:{clientId}] Логин: аккаунт='{account}', пароль='{password}'");
                
                // После логина отправляем список персонажей
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
                
                // Читаем имя
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

                // Успех
                await Send(s, OP_CREATE_CHAR_SUCCESS, new byte[] { 0x01 }, clientId);
                
                // Отправляем обновленный список персонажей
                await Task.Delay(100);
                await SendCharList(s, clientId);
                
                // Отправляем тестовые пакеты как в APPPROC.lua
                await Task.Delay(100);
                await SendExpChange(s, clientId, 100); // +100 опыта
                await Task.Delay(100);
                await SendHarm(s, clientId, 50); // урон 50
            }
            catch
            {
                await Send(s, OP_CREATE_CHAR_SUCCESS, new byte[] { 0x00 }, clientId);
            }
        }

        private static async Task SendServerList(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка списка серверов (MSG_APP_REPLY_SERVER_LIST)");
            
            // Формат как в APPPROC.lua строка 105
            // r8_1 = количество серверов
            // для каждого: имя сервера, количество подсерверов
            // для каждого подсервера: имя, количество IP, список IP
            
            var ms = new System.IO.MemoryStream();
            var writer = new System.IO.BinaryWriter(ms);
            
            // Количество серверов (1)
            writer.Write(1);
            
            // Имя первого сервера
            string serverName = "Тестовый сервер";
            writer.Write(serverName.Length + 1);
            writer.Write(Encoding.UTF8.GetBytes(serverName));
            writer.Write((byte)0);
            
            // Количество подсерверов (2)
            writer.Write(2);
            
            // Подсервер 1
            string subName1 = "Подсервер 1";
            writer.Write(subName1.Length + 1);
            writer.Write(Encoding.UTF8.GetBytes(subName1));
            writer.Write((byte)0);
            
            // Количество IP для подсервера 1 (1)
            writer.Write(1);
            // IP адрес
            string ip1 = "127.0.0.1";
            writer.Write(ip1.Length + 1);
            writer.Write(Encoding.UTF8.GetBytes(ip1));
            writer.Write((byte)0);
            
            // Подсервер 2
            string subName2 = "Подсервер 2";
            writer.Write(subName2.Length + 1);
            writer.Write(Encoding.UTF8.GetBytes(subName2));
            writer.Write((byte)0);
            
            // Количество IP для подсервера 2 (1)
            writer.Write(1);
            string ip2 = "127.0.0.1";
            writer.Write(ip2.Length + 1);
            writer.Write(Encoding.UTF8.GetBytes(ip2));
            writer.Write((byte)0);
            
            byte[] payload = ms.ToArray();
            await Send(s, OP_SERVER_LIST, payload, clientId);
        }

        private static async Task SendAccountSave(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка сохраненного аккаунта (MSG_APP_ACCOUNT_SAVE)");
            
            // Формат как в APPPROC.lua строка 152
            var ms = new System.IO.MemoryStream();
            var writer = new System.IO.BinaryWriter(ms);
            
            // saveAccount checkbox (1 = сохранен)
            writer.Write((byte)1);
            
            // Имя аккаунта
            string account = "test";
            writer.Write(account.Length + 1);
            writer.Write(Encoding.UTF8.GetBytes(account));
            writer.Write((byte)0);
            
            // serverId (0 = первый сервер)
            writer.Write(0);
            
            // selectIndex (0 = первый персонаж)
            writer.Write(0);
            
            byte[] payload = ms.ToArray();
            await Send(s, OP_ACCOUNT_SAVE, payload, clientId);
        }

        private static async Task SendLoginSuccess(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка LoginSuccess (0x000B)");
            
            var payload = new byte[772];
            payload[0x28] = 0x01; // Флаг успеха
            
            await Send(s, OP_LOGIN_SUCCESS, payload, clientId);
        }

        private static async Task SendCharList(NetworkStream s, int clientId)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка CharList (0x001A)");
            
            // Пустой список персонажей
            var payload = new byte[68];
            payload[0] = 0x00; // 0 персонажей
            
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

        private static async Task SendExpChange(NetworkStream s, int clientId, int exp)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка изменения опыта (MSG_APP_EXP_CHANGE)");
            
            // Формат как в APPPROC.lua строка 366
            var ms = new System.IO.MemoryStream();
            var writer = new System.IO.BinaryWriter(ms);
            
            // ID персонажа (0 = главный)
            writer.Write(0);
            
            // Количество опыта
            writer.Write(exp);
            
            byte[] payload = ms.ToArray();
            await Send(s, OP_EXP_CHANGE, payload, clientId);
        }

        private static async Task SendHarm(NetworkStream s, int clientId, int damage)
        {
            Console.WriteLine($"[Server:{clientId}] >>> Отправка урона (MSG_APP_HARM)");
            
            // Формат как в APPPROC.lua строка 312
            var ms = new System.IO.MemoryStream();
            var writer = new System.IO.BinaryWriter(ms);
            
            // ID цели
            writer.Write(0);
            
            // ID атакующего
            writer.Write(1);
            
            // Количество урона
            writer.Write(damage);
            
            // Тип урона (0 = обычный)
            writer.Write((byte)0);
            
            // Флаги (0)
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