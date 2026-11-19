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

    // ТОЧНОЕ ПРИВЕТСТВИЕ из дампа (31 байт)
    public async Task SendHandshake()
    {
        byte[] handshake = new byte[]
        {
            0x1F, 0x00, // длина 31
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

        ms.Write(BitConverter.GetBytes((ushort)0), 0, 2); // длина
        ms.Write(BitConverter.GetBytes((ushort)0x0065), 0, 2); // opcode ServerList

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
        packet[0] = (byte)(len & 0xFF);
        packet[1] = (byte)((len >> 8) & 0xFF);

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

    public async Task Run()
    {
        Console.WriteLine($"Клиент подключился: {client.Client.RemoteEndPoint}");

        try
        {
            await SendHandshake();
            Console.WriteLine("Handshake отправлен");

            while (client.Connected)
            {
                int size = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (size <= 0)
                    break;

                Console.WriteLine($"Получено {size} байт");

                if (size >= 4)
                {
                    ushort packetLen = (ushort)(buffer[0] | (buffer[1] << 8));
                    ushort opcode = (ushort)(buffer[2] | (buffer[3] << 8));

                    Console.WriteLine($"Длина пакета: {packetLen}, Opcode: 0x{opcode:X4}");

                    switch (opcode)
                    {
                        case (ushort)Opcode.Heartbeat:
                            Console.WriteLine("Heartbeat получен");
                            break;

                        case (ushort)Opcode.Login:
                            Console.WriteLine("Login пакет получен!");
                            await LoginHandler.Handle(this, buffer);
                            break;

                        default:
                            Console.WriteLine($"Неизвестный opcode: 0x{opcode:X4}");
                            break;
                    }
                }
                else if (size == 2)
                {
                    Console.WriteLine("Heartbeat (2 байта)");
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