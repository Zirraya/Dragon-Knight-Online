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

    internal static byte[] CreateQuestMarker(int npcId, bool available) =>
        CreateQuestMarker(npcId, available ? (byte)1 : (byte)0);

    // Marker state 1 is an exclamation mark, state 2 is a gray question,
    // and state 3 is a yellow question.
    internal static byte[] CreateQuestMarker(int npcId, byte state) => Build(Opcode.QuestMarker, writer =>
    {
        WriteInt32(writer, npcId);
        WriteUInt16(writer, state);
        writer.Write((byte)(state == 0 ? 0 : 1));
        WriteInt32(writer, Environment.TickCount);
    });

    internal static byte[] CreateQuestDetails(WorldQuest quest, QuestProgress progress, int state)
        => Build(Opcode.QuestDetails, writer =>
        {
            WriteInt32(writer, quest.Id);
            WriteQuestString(writer, quest.Title);
            WriteUInt16(writer, quest.Level);
            WriteQuestString(writer, quest.Description);
            string targetName = quest.TargetName is null
                ? string.Empty
                : quest.TargetX > 0 && quest.TargetY > 0
                    ? $" Цель: {quest.TargetName} (X: {quest.TargetX / 100}, Y: {quest.TargetY / 100})."
                    : $" Цель: {quest.TargetName}.";
            string progressText = quest.KillCount > 0
                ? $" Убито: {Math.Clamp(progress.Kills, 0, quest.KillCount)}/{quest.KillCount}."
                : string.Empty;
            string objective = quest.Objective + progressText + targetName;
            WriteQuestString(writer, objective);
            writer.Write((byte)0);
            WriteInt32(writer, state);
            // The client expects a fixed details section after the objective.
            // The first quest uses the shorter 234-byte section captured in
            // the client trace. Other quest records use 257 bytes.
            // Every captured quest-details packet reserves five objective
            // records, including quests without a map link. The client uses
            // this count when it attaches the progress packet to the journal.
            WriteUInt16(writer, 5);
            int tailPadding = quest.Id == 114001 ? 223 : 246;
            WriteQuestTail(writer, quest, tailPadding);
            WriteInt32(writer, Environment.TickCount);
        });

    private static void WriteQuestTail(BinaryWriter writer, WorldQuest quest, int length)
    {
        using var objectives = new MemoryStream();
        using (var objectiveWriter = new BinaryWriter(objectives, QuestEncoding, leaveOpen: true))
        {
            for (int index = 0; index < 5; index++)
            {
                bool target = index == 0 && quest.TargetName is not null;
                WriteInt32(objectiveWriter, target ? quest.TargetX / 100 : 0);
                WriteInt32(objectiveWriter, target ? quest.TargetY / 100 : 0);
                WriteInt32(objectiveWriter, target ? quest.Monster == 0 ? 8 : 16 : 0);
                WriteQuestString(objectiveWriter, target ? quest.TargetName! : string.Empty);
            }
        }

        byte[] objectiveBytes = objectives.ToArray();
        writer.Write(objectiveBytes);

        var rewardItems = quest.RewardItems.Take(6).ToArray();
        bool hasChoices = rewardItems.Length > 1;
        int rewardHeaderLength = hasChoices ? 14 : 16;
        int fixedItemMetadataLength = hasChoices ? 12 : 14;
        int rewardSectionLength = rewardHeaderLength + 4 + 2 + fixedItemMetadataLength
            + Math.Max(0, rewardItems.Length - 1) * 6 + 2 * sizeof(int);
        int padding = Math.Max(0, length - objectiveBytes.Length - rewardSectionLength);
        WriteUInt16(writer, 1);
        writer.Write(new byte[rewardHeaderLength - sizeof(ushort)]);
        WriteInt32(writer, rewardItems[0]);
        WriteUInt16(writer, quest.RewardItemCount);
        WriteUInt16(writer, 1);
        WriteInt32(writer, 1);
        WriteInt32(writer, 0);
        if (!hasChoices)
        {
            WriteUInt16(writer, 0x0C00);
            WriteUInt16(writer, 0x0100);
        }
        else
            writer.Write(new byte[2]);
        foreach (ushort itemId in rewardItems.Skip(1))
        {
            WriteInt32(writer, itemId);
            WriteUInt16(writer, quest.RewardItemCount);
        }
        WriteInt32(writer, quest.EffectiveExperience);
        WriteInt32(writer, quest.Reward);
        writer.Write(new byte[padding]);

        int written = objectiveBytes.Length + rewardSectionLength + padding;
        if (written < length)
            writer.Write(new byte[length - written]);
    }

    internal static byte[] CreateQuestOffer(WorldQuest quest, QuestProgress progress, bool completed)
        => Build(completed ? Opcode.QuestTurnIn : Opcode.QuestOffer, writer =>
        {
            WriteInt32(writer, quest.Id);
            string text = quest.Id == 114001
                ? quest.Objective
                : completed ? $"Готово: {quest.Title}" : quest.Title;
            WriteQuestString(writer, text);
            WriteInt32(writer, Environment.TickCount);
        });

    internal static byte[] CreateQuestProgress(WorldQuest quest, QuestProgress progress) => Build(Opcode.QuestProgress, writer =>
    {
        // 0x0E60 contains one record for each objective. The objective type
        // is big-endian, while the target template ID is a little-endian
        // field in the client protocol. The final byte is the current count.
        if (quest.Monster == 0)
        {
            WriteUInt16(writer, 0);
            return;
        }

        WriteUInt16(writer, 1);
        WriteInt32(writer, quest.Id);
        WriteInt32(writer, 0);
        writer.Write(BitConverter.GetBytes(quest.Monster));
        writer.Write((byte)Math.Clamp(progress.Kills, 0, byte.MaxValue));
    });

    // The journal packet only registers the quest ID. Details and progress
    // are sent separately through QuestDetails and QuestProgress.
    internal static byte[] CreateQuestJournal(WorldQuest quest, QuestProgress progress) =>
        Build(Opcode.QuestJournal, writer =>
        {
            WriteInt32(writer, quest.Id);
            WriteInt32(writer, Environment.TickCount);
        });

    // 0x0E5A is the task-window list. It is a sequence of quest ID/title
    // pairs followed by a zero field and a timestamp.
    internal static byte[] CreateQuestList(IReadOnlyList<(WorldQuest Quest, QuestProgress Progress)> quests) =>
        Build(Opcode.WorldClock, writer =>
        {
            foreach (var (quest, _) in quests)
            {
                WriteInt32(writer, quest.Id);
                WriteQuestString(writer, quest.Title);
            }
            WriteInt32(writer, 0);
            WriteInt32(writer, Environment.TickCount);
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
