using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;
using DragonKnightServer.Database;
using DragonKnightServer.Handlers;
using DragonKnightServer.Protocol;

namespace DragonKnightServer;

public class ClientSession
{
    private readonly TcpClient client;
    private readonly NetworkStream stream;
    private readonly byte[] buffer = new byte[8192];
    private int accountId;
    private WorldSession? world;
    private bool closeAfterResponse;

    internal void CloseAfterResponse() => closeAfterResponse = true;

    private void LeaveWorld()
    {
        world?.SavePosition(AccountId);
        world = null;
    }

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

    public Task SendHandshake()
    {
        var packet = new byte[31];
        LoginPackets.WriteHeader(packet, Opcode.Handshake);
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(8), 17);

        // В захватах после длины строки идут 16 букв или цифр и нулевой байт.
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        for (int index = 10; index < 26; index++)
        {
            packet[index] = (byte)alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return Send(packet);
    }

    public Task SendCharacterList(IReadOnlyList<LoginCharacter> characters)
    {
        return Send(LoginPackets.CreateCharacterList(characters));
    }

    internal Task SendPackets(byte[][] packets, bool enteringWorld = false)
    {
        if (packets.Length == 0)
            return Task.CompletedTask;
        if (packets.Length == 1)
            return Send(packets[0]);

        byte[] batch = new byte[packets.Sum(packet => packet.Length)];
        int offset = 0;
        foreach (byte[] packet in packets)
        {
            packet.CopyTo(batch, offset);
            offset += packet.Length;
        }

        if (enteringWorld)
        {
            var trace = packets.Select(packet =>
                $"0x{BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(LoginPackets.OpcodeOffset)):X4}/{packet.Length}");
            Console.WriteLine($"[World] Пакеты запуска карты (код/длина): {string.Join(", ", trace)}");
        }
        return Send(batch);
    }

    private async Task Dispatch(byte[] packet, int size)
    {
        if (world is not null)
            await SendPackets(world.CollectCreatureRespawns(Environment.TickCount64));

        if (size == 2)
        {
            Console.WriteLine("Heartbeat (2 байта)");
            return;
        }

        if (size < LoginPackets.HeaderSize)
        {
            Console.WriteLine($"Слишком короткий заголовок: {size} байт");
            return;
        }

        ushort opcode = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(LoginPackets.OpcodeOffset));
        Console.WriteLine($"Длина пакета: {size}, Opcode: 0x{opcode:X4}");

        switch (opcode)
        {
            case (ushort)Opcode.Login:
                LeaveWorld();
                Console.WriteLine("Login пакет получен!");
                await LoginHandler.Handle(this, packet, size);
                break;

            case (ushort)Opcode.CreateCharacter:
                await CreateCharacterHandler.Handle(this, packet);
                break;

            case (ushort)Opcode.DeleteCharacter when world is null:
                await Send(LoginPackets.CreateResult(Opcode.DeleteCharacterResult,
                    CharacterRepository.TryDelete(AccountId, LoginPackets.ReadCharacterName(packet)) ? (ushort)0 : (ushort)1));
                break;

            case (ushort)Opcode.SelectCharacter:
                LeaveWorld();
                world = await WorldEntryHandler.Handle(this, packet);
                break;

            case (ushort)Opcode.ReturnToCharacterList when AccountId > 0 && size == LoginPackets.HeaderSize:
                LeaveWorld();
                var characters = CharacterRepository.GetForLogin(AccountId, LoginPackets.CharacterSlots);
                // Передаём список вместе со сменой сцены и обновляем его после перехода.
                await Send(LoginPackets.CreateCharacterList(characters, Opcode.ReturnToCharacterListResult));
                await Task.Delay(1200);
                await SendCharacterList(characters);
                Console.WriteLine("Отправлен список персонажей после выхода из мира.");
                break;

            case (ushort)Opcode.WorldClockRequest when world is not null && size == LoginPackets.HeaderSize:
                await Send(WorldPackets.CreateClock());
                break;

            case (ushort)Opcode.Move when world is not null && size == WorldPackets.MoveRequestSize:
                await SendPackets(world.Move(packet));
                break;

            case (ushort)Opcode.UseSkill when world is not null && size is 15 or 19:
                await SendPackets(world.UseSkill(packet));
                break;

            case (ushort)Opcode.CallDragon when world is not null && size is 16 or 20:
                await SendPackets(world.Call(packet));
                break;

            case (ushort)Opcode.SetDragonSlot when world is not null && size == 13:
                await SendPackets(world.SetDragonSlot(packet));
                break;

            case (ushort)Opcode.RequestDragonSlots when world is not null && size == LoginPackets.HeaderSize:
                await Send(world.GetDragonSlots());
                break;

            case (ushort)Opcode.MoveBackpackItem when world is not null && size == 16:
                await SendPackets(world.MoveBackpackItem(packet));
                break;

            case (ushort)Opcode.ToggleEquipment when world is not null && size >= 13:
                await SendPackets(world.ToggleEquipment(packet));
                break;

            case (ushort)Opcode.MovementAction when world is not null && size is 13 or 19:
                await SendPackets(world.Stop(packet));
                break;

            case (ushort)Opcode.SelectTarget when world is not null && size == 12:
                break;

            case (ushort)Opcode.Turn when world is not null && size == 16:
                break;

            case (ushort)Opcode.ClientQuickbar when world is not null:
                world.SaveQuickbar(packet);
                break;

            // This client build sends a 20-byte TalkToNpc packet. Older
            // captures contain four additional trailing bytes, so accept
            // both layouts.
            case (ushort)Opcode.TalkToNpc when world is not null && size >= 20:
                await SendPackets(world.HandleNpcPacket(packet));
                break;

            // UI and world-state notifications sent by the current client.
            // They carry no server response in the captured protocol.
            case 0x084E when world is not null && size == 8:
            case 0x10CB when world is not null && size == 9:

            case (ushort)Opcode.RequestQuestMarkers when world is not null && size == 8:
                // In the captured client flow 0x0A59 is sent when the quest
                // journal is opened. The server answers with 0x0E5A and the
                // quest data, not with 0x0E5F NPC markers.
                await SendPackets(world.QuestJournalPackets());
                break;

            case (ushort)Opcode.RequestQuestDetails when world is not null && size == 12:
            case (ushort)Opcode.AcceptQuest when world is not null && size == 12:
            case (ushort)Opcode.AbandonQuest when world is not null && size == 12:
            case (ushort)Opcode.RequestQuestJournal when world is not null && size >= LoginPackets.HeaderSize:
                await SendPackets(world.HandleQuestPacket((Opcode)opcode, packet));
                break;

            case (ushort)Opcode.RequestQuestTracking when world is not null && size == 13:
                await SendPackets(world.HandleQuestPacket(Opcode.RequestQuestTracking, packet));
                break;

            case (ushort)Opcode.RequestShopBuyback when world is not null && size == 20:
                break;

            case (ushort)Opcode.CompleteQuest when world is not null && size == 14:
                await SendPackets(world.HandleQuestPacket(Opcode.CompleteQuest, packet));
                break;

            case (ushort)Opcode.BuyFromNpc when world is not null && size == 19:
                await SendPackets(world.BuyFromDragonEnvoy(packet));
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
            Task<int>? receive = null;
            while (client.Connected)
            {
                receive ??= stream.ReadAsync(buffer, have, buffer.Length - have);
                if (world?.BattleDragonCastDelay is { } castDelay)
                {
                    await Task.WhenAny(receive, castDelay);
                    if (castDelay.IsCompletedSuccessfully)
                        await SendPackets(world.CompleteBattleDragonCast());
                    if (!receive.IsCompleted)
                        continue;
                }

                int read = await receive;
                receive = null;
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
                    if (world?.BattleDragonCastDelay?.IsCompletedSuccessfully == true)
                        await SendPackets(world.CompleteBattleDragonCast());
                    await Dispatch(slice, packetLen);
                    if (closeAfterResponse)
                        return;
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
            LeaveWorld();
            client.Close();
            Console.WriteLine("Клиент отключился");
        }
    }
}
