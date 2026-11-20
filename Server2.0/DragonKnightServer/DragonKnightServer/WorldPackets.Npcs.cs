using System.Buffers.Binary;
using System.Text;
using DragonKnightServer.Database;

namespace DragonKnightServer.Protocol;

internal static partial class WorldPackets
{
    private static readonly Encoding QuestEncoding = LoginPackets.NameEncoding;

    internal static byte[] CreateNpcDialog(int npcId, string category, IReadOnlyList<(ushort Code, string Title)> options)
        => Build(Opcode.NpcDialog, writer =>
        {
            WriteUInt16(writer, 0);
            WriteInt32(writer, npcId);
            WriteInt32(writer, 1);
            WriteQuestString(writer, category);
            WriteInt32(writer, options.Count);
            foreach (var (code, title) in options)
            {
                WriteUInt16(writer, code);
                WriteQuestString(writer, title);
            }
            WriteInt32(writer, Environment.TickCount);
        });

    internal static byte[] CloseNpcDialog() => Build(Opcode.NpcDialog, writer => WriteUInt16(writer, 3));

    internal static byte[] CreateQuestMarker(int npcId, bool available) => Build(Opcode.QuestMarker, writer =>
    {
        WriteInt32(writer, npcId);
        WriteUInt16(writer, available ? (ushort)1 : (ushort)0);
        writer.Write((byte)(available ? 1 : 0));
        WriteInt32(writer, Environment.TickCount);
    });

    internal static byte[] CreateQuestDetails(WorldQuest quest, QuestProgress progress, int state)
        => Build(Opcode.QuestDetails, writer =>
        {
            WriteInt32(writer, quest.Id);
            WriteQuestString(writer, quest.Title);
            WriteUInt16(writer, quest.Level);
            WriteQuestString(writer, quest.Description);
            string objective = quest.KillCount == 0
                ? quest.Objective
                : $"{quest.Objective} ({progress.Kills}/{quest.KillCount})";
            WriteQuestString(writer, objective);
            writer.Write((byte)0);
            WriteInt32(writer, state);
            WriteUInt16(writer, 0);
            WriteQuestString(writer, string.Empty);
            WriteInt32(writer, quest.Reward);
            writer.Write((byte)(state == 1 ? 1 : 0));
            WriteUInt16(writer, 0);
            WriteUInt16(writer, 0);
            WriteUInt16(writer, 0);
            WriteUInt16(writer, 0);
            for (int i = 0; i < 36; i++)
                WriteUInt16(writer, 0);
            WriteInt32(writer, Environment.TickCount);
        });

    internal static byte[] CreateQuestOffer(WorldQuest quest, QuestProgress progress, bool completed)
        => Build(completed ? Opcode.QuestTurnIn : Opcode.QuestOffer, writer =>
        {
            WriteInt32(writer, quest.Id);
            WriteQuestString(writer, completed ? $"Готово: {quest.Title}" : quest.Title);
            WriteInt32(writer, Environment.TickCount);
        });

    internal static byte[] CreateQuestProgress(WorldQuest quest, QuestProgress progress) => Build(Opcode.QuestProgress, writer =>
    {
        WriteUInt16(writer, 1);
        WriteInt32(writer, quest.Id);
        writer.Write((byte)Math.Clamp(progress.Kills, 0, byte.MaxValue));
        WriteInt32(writer, progress.Kills);
        WriteInt32(writer, quest.KillCount);
    });

    internal static byte[] CreateNpcShop(int npcId, int infoId)
        => Build(Opcode.NpcShop, writer =>
        {
            WriteInt32(writer, npcId);
            WriteInt32(writer, infoId);
            WriteQuestString(writer, "Посланник Драконов");
            WriteInt32(writer, 0);
            WriteUInt16(writer, (ushort)WorldQuests.DragonEnvoyStock.Count);
            foreach (var item in WorldQuests.DragonEnvoyStock)
            {
                WriteInt32(writer, item.ItemId);
                WriteUInt16(writer, item.StackLimit);
                WriteInt32(writer, 0);
                WriteUInt16(writer, 0);
                WriteUInt16(writer, checked((ushort)item.Price));
            }
            WriteInt32(writer, Environment.TickCount);
        });

    internal static byte[] CreateShopResult(bool success, int itemId, int price)
        => Build(Opcode.NpcShopResult, writer =>
        {
            WriteInt32(writer, itemId);
            WriteInt32(writer, price);
            WriteUInt16(writer, success ? (ushort)1 : (ushort)0);
        });

    // The client listens for 0x0F33 after BuyFromNpc. 0x0E6D is not the
    // purchase acknowledgement used by the captured client build.
    internal static byte[] CreateShopPurchaseResult(bool success, int itemId, int price)
        => Build(Opcode.ShopPurchaseResult, writer =>
        {
            WriteInt32(writer, itemId);
            WriteInt32(writer, price);
            WriteUInt16(writer, success ? (ushort)1 : (ushort)0);
        });

    private static void WriteQuestString(BinaryWriter writer, string value)
    {
        byte[] bytes = QuestEncoding.GetBytes(value);
        WriteUInt16(writer, checked((ushort)(bytes.Length + 1)));
        writer.Write(bytes);
        writer.Write((byte)0);
    }
}
