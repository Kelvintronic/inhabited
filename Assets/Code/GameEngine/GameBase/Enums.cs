namespace GameEngine
{
    public enum ObjectType
    {
        // common to PlayerBagItem
        None,
        Bomb,
        Health,
        KeyRed,
        KeyGreen,
        KeyBlue,

        // extended
        Wall,
        ExitPoint,
        NPC_Intent,
        NPCBug,
        NPCTrader,
        NPCMercenary,
        BugNest,
        Cash,
        Heart,
        Door,
        DoorRed,
        DoorGreen,
        DoorBlue,
        HiddenDoor,
        Chest,
        FalseWall,

        // server only objects
        TelePort
    }

    public enum NPCStance
    {
        Aggressive,
        Neutral,
        Ally
    }

    public enum PlayerBagItem
    {
        Lint, // null or nothing
        Bomb,
        Health,
        KeyRed,
        KeyGreen,
        KeyBlue,
    }
}