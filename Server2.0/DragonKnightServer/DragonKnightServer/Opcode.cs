namespace DragonKnightServer.Protocol;


public enum Opcode : ushort
{
    Heartbeat = 0x0200,

    Login = 0x0064,
    ServerList = 0x0065,

    LoginSuccess = 0x000B,

    CharacterList = 0x001A,
    CreateCharacter = 0x006A,
    CreateCharacterResult = 0x006B,


    EnterWorld = 0x0017,
    WorldInfo = 0x0018,

    PlayerStatus = 0x0019,

    Move = 0x0020
}