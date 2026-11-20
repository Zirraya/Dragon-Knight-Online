using DragonKnightServer.Database;
using DragonKnightServer.Protocol;

namespace DragonKnightServer.Handlers;

internal static class WorldEntryHandler
{
    public static async Task<WorldSession?> Handle(ClientSession client, byte[] packet)
    {
        string? name = LoginPackets.ReadCharacterName(packet);
        if (client.AccountId <= 0 || name is null)
        {
            Console.WriteLine("Выбор персонажа отклонён: требуется вход и корректное имя.");
            return null;
        }

        var character = CharacterRepository.FindForWorld(client.AccountId, name);
        if (character is null)
        {
            Console.WriteLine($"Персонаж не найден для аккаунта {client.AccountId}.");
            return null;
        }

        var world = new WorldSession(character);
        await client.SendPackets([.. WorldPackets.CreateEntry(character), .. world.UpdateNearbyWorldActors(), .. world.QuestStatePackets()], enteringWorld: true);
        Console.WriteLine($"Отправлена загрузка карты {character.Map} для персонажа {character.Name}.");
        return world;
    }

}
