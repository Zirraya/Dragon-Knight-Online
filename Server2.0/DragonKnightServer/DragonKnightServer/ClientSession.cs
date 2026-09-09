using System.Net.Sockets;
using DragonKnightServer.Handlers;
using DragonKnightServer.Protocol;

namespace DragonKnightServer;

public class ClientSession
{
    private readonly TcpClient client;
    private readonly NetworkStream stream;
    private readonly byte[] buffer = new byte[8192];
    private int accountId;

    public int AccountId
    {
        get => accountId;
        set => accountId = value;
    }

    public ClientSession(TcpClient client)
    {
        this.client = client;
        this.stream = client.GetStream();
    }

    public async Task Send(byte[] data)
    {
        try
        {
            await stream.WriteAsync(data, 0, data.Length);
            Console.WriteLine($"[Server] >>> Отправлен пакет {data.Length} байт");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка отправки: {ex.Message}");
        }
    }

    // ПРИВЕТСТВИЕ из дампа (31 байт). Реальный payload в текстовых логах отсутствует —
    // здесь только правильная длина в BIG-ENDIAN (00 1F). Содержимое нужно снять из дампа.
    public async Task SendHandshake()
    {
        byte[] handshake = new byte[]
        {
            0x00, 0x1F, // длина 31 (big-endian)
            0x00, 0x00, // unknown
            0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00
        };
        await Send(handshake);
    }

    // ОТВЕТ НА ЛОГИН с сервером (как в пакете #18)
    public async Task SendServerList()
    {
        Console.WriteLine("[Server] >>> Отправка ServerList (0x0065)");

        using var ms = new MemoryStream();

        ms.Write(new byte[] { 0, 0 }, 0, 2);      // длина (big-endian, заполним в конце)
        ms.WriteByte(0x00); ms.WriteByte(0x65);   // opcode ServerList (big-endian)

        ms.WriteByte(1); // количество серверов
        ms.WriteByte(0); // ID сервера

        string serverName = "Test Server";
        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(serverName);
        ms.Write(nameBytes, 0, nameBytes.Length);
        ms.WriteByte(0); // null-терминатор

        ms.WriteByte(0); // статус
        ms.WriteByte(0); // тип
        ms.WriteByte(0); // населенность

        ms.WriteByte(127);
        ms.WriteByte(0);
        ms.WriteByte(0);
        ms.WriteByte(1);

        // ПОРТ 3800
        ms.Write(BitConverter.GetBytes((ushort)3800), 0, 2);

        ms.WriteByte(0);
        ms.WriteByte(0);
        ms.WriteByte(0);
        ms.WriteByte(0);

        var packet = ms.ToArray();
        ushort len = (ushort)packet.Length;
        packet[0] = (byte)((len >> 8) & 0xFF); // big-endian
        packet[1] = (byte)(len & 0xFF);

        await Send(packet);
    }

    // Ответ на выбор сервера (пакет #65 - 35 байт)
    public async Task SendServerSelectResponse()
    {
        byte[] response = new byte[]
        {
            0x23, 0x00, // длина 35
            0x00, 0x00, // unknown opcode?
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00
        };
        await Send(response);
    }

    private async Task Dispatch(byte[] packet, int size)
    {
        if (size == 2)
        {
            Console.WriteLine("Heartbeat (2 байта)");
            return;
        }

        ushort opcode = (ushort)((packet[2] << 8) | packet[3]); // big-endian
        Console.WriteLine($"Длина пакета: {size}, Opcode: 0x{opcode:X4}");

        switch (opcode)
        {
            case (ushort)Opcode.Heartbeat:
                Console.WriteLine("Heartbeat получен");
                break;

            case (ushort)Opcode.Login:
                Console.WriteLine("Login пакет получен!");
                await LoginHandler.Handle(this, packet, size);
                break;

            default:
                Console.WriteLine($"Неизвестный opcode: 0x{opcode:X4}");
                break;
        }
    }

    public async Task Run()
    {
        Console.WriteLine($"Клиент подключился: {client.Client.RemoteEndPoint}");

        try
        {
            await SendHandshake();
            Console.WriteLine("Handshake отправлен");

            // TCP — это поток, а не сообщения. Один ReadAsync может принести половину
            // пакета или сразу несколько (в дампе сервер шлёт десятки сегментов подряд).
            // Поэтому копим байты и нарезаем их по длине из заголовка (big-endian).
            int have = 0;
            while (client.Connected)
            {
                int read = await stream.ReadAsync(buffer, have, buffer.Length - have);
                if (read <= 0)
                    break;

                have += read;
                Console.WriteLine($"Получено {read} байт (в буфере {have})");

                int consumed = 0;
                while (have - consumed >= 2)
                {
                    // длина всего пакета в BIG-ENDIAN
                    ushort packetLen = (ushort)((buffer[consumed] << 8) | buffer[consumed + 1]);

                    if (packetLen < 2 || packetLen > buffer.Length)
                    {
                        Console.WriteLine($"Некорректная длина пакета: {packetLen}, разрыв соединения");
                        return;
                    }
                    if (have - consumed < packetLen)
                        break; // пакет пришёл не целиком — ждём остаток

                    var slice = new byte[packetLen];
                    Array.Copy(buffer, consumed, slice, 0, packetLen);
                    await Dispatch(slice, packetLen);
                    consumed += packetLen;
                }

                // сдвигаем «хвост» (недочитанный пакет) в начало буфера
                if (consumed > 0)
                {
                    Array.Copy(buffer, consumed, buffer, 0, have - consumed);
                    have -= consumed;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine("Клиент отключился");
        }
    }
}