namespace GameEngine
{
    public enum Flag
    {
        None,
        IsHostile
    }

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
        NPCSpider,
        NPCMantis,
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
        FalseWall,
        Barricade,

        // Container layer objects
        Chest,

        // Functional layer objects
        Conveyor
    }
     public enum ObjectLayer
    {
        Main,           // Main level object and NPC layer

        // objects not on Main can be walked over by NPCs
        Container,      // Container Layer (for chests and searchable objects)
        Funcion         // Objects that are static and act on players
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