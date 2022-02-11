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
        NPCMercenary,
        NPCTrader,
        BugNest,
        Cash,
        Heart,
        Door,
        DoorRed,
        DoorGreen,
        DoorBlue,
        HiddenDoor,
        Chest,
        FalseWall
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