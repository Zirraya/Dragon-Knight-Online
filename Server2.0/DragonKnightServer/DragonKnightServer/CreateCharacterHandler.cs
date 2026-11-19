using System.Buffers.Binary;
using DragonKnightServer.Database;
using DragonKnightServer.Protocol;

namespace DragonKnightServer.Handlers;

internal static class CreateCharacterHandler
{
    public static async Task Handle(ClientSession client, byte[] packet)
    {
        bool created = client.AccountId > 0 && TryCreate(client.AccountId, packet);

        var response = new byte[10];
        LoginPackets.WriteHeader(response, Opcode.CreateCharacterResult);
        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(LoginPackets.HeaderSize), created ? (ushort)0 : (ushort)1);
        await client.Send(response);
    }

    private static bool TryCreate(int accountId, ReadOnlySpan<byte> packet)
    {
        ReadOnlySpan<byte> body = packet[LoginPackets.HeaderSize..];
        if (!LoginPackets.TryReadString(ref body, out var nameBytes)
            || nameBytes.Length is < 1 or > 32
            || body.Length < sizeof(int) + LoginPackets.AppearanceSize)
        {
            return false;
        }

        string name = LoginPackets.NameEncoding.GetString(nameBytes);
        int template = BinaryPrimitives.ReadInt32BigEndian(body);
        var appearance = body.Slice(sizeof(int), LoginPackets.AppearanceSize);
        body = body[(sizeof(int) + LoginPackets.AppearanceSize)..];

        // После внешности идёт ещё одна строка. Карту для нового персонажа выбирает сервер.
        if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsControl) || template <= 0
            || !LoginPackets.TryReadString(ref body, out _) || !body.IsEmpty)
        {
            return false;
        }

        try
        {
            bool created = CharacterRepository.TryCreate(accountId, name, template, appearance.ToArray(),
                LoginPackets.CharacterSlots);
            Console.WriteLine($"Создание персонажа для аккаунта {accountId}: {(created ? "успешно" : "отклонено")}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка создания персонажа: {ex.Message}");
            return false;
        }
    }

}
