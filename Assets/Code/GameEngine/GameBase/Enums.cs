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
        NPC_level1,
        NPC_level2,
        NPC_level3,
        Gen_level1,
        Gen_level2,
        Gen_level3,
        Cash,
        Heart,
        DoorRed,
        DoorGreen,
        DoorBlue,
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