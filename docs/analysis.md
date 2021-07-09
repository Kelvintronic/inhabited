## Analysis

This file is an analysis of the game using the following format:
 - Uncertainties (decisions to be made - will be filtered down when made)
 - Requirements (things the game is required to do)
 - Description (in natural language what the game is)
 - Objects (any significant objects that can be recognised based on the Requirements and Description)
 - Interactions (descriptions of how objects will interact)
 - Class diagram (external to this document)

Note: This is not a final spec. Everything here is fluid.

### Uncertainties

This section is added to highlight and uncertainties that need to be determined or agreed upon. Once decided on these sections will take their place in the other sections. Agree to disagree or whatever.

**Game play loop? Object messaging?**

How will actual play be iterated?

The original Dandy game updated everything on a loop in a hierarchical order. The new game may need to be a little more complicated.

Do we implement object messaging? Do we only need a central messaging system or do we need to allow peer object messaging? Perhaps all messages must pass through the central filter and are then passed down if required?

I would like to have a speed attribute attached to characters and machines that will add a delay to the delivery of messages. This would allow faster characters to move and attack more quickly. Also this would mean that when two characters with differing speeds activate the same object (e.g: a switch) at the same moment, the faster of the two would activate it first. Subsequent to this any activation messages should have a state assigned such that objects cannot be activated twice resulting in player frustration!

**How to represent the map?**

First off, I think the game should be two dimensional only - no stairs or upper stories on the same map. Upper stories should be separate maps.

In the original dandy game players, monster, objects and statics all lived in a single matrix for a level. In the new game, I think characters (player and non-player) need to be held on an overlay. This is not to suggest that either layer needs to be static. This is only to allow multiple objects to appear on a single screen location. The lowest overlay should contain static objects (such as walls), items (things that can be picked up) and doors or portals. The next layer should contain characters. Potentially another layer may contain machines. The idea being that the lowest level contains no actors and the higher layers can use or move items on the lower levels.

E.g: If an object is on the first level a character on the second level can pick up the object and drop it elsewhere. And a machine let's say a conveyor belt on the third level can move a character or an object on either of the first or second layers.

Perhaps another way to do this is to code each matrix location as a collection of objects??

**What are the minimum controls?**

The controls should be simple enough to translate to use on a gaming console or phone.

Here's a PC suggestion:

  - Mouse: Change the characters direction of view (360°)
  - Mouse left: Activate weapon
  - Mouse right: Activate object or spell as assigned by player
  - A, S, D, W: Player movement
  - Space Bar (hold down): Release mouse from movement and allow user to interact with inventory (which is already in view and part of the window frame). The player can then use, drop or assign spells or objects.
  - Space Bar (tap): Pick up object
  - E: Activate in world static object (door, switch or chest) or communicate with another character. Player must be adjacent to and facing the object or character.

Potentially on a phone we could emulate a game controller.

### Requirements

**Application**  
Single or Multiplayer (online or LAN)
All players in level at once
Staging area leading to dungeon levels (sets)
Simple small dungeon levels
When a character leaves a level they cannot return. They are therefor in 'transit'.
The level does not end until all players have left the level.
If a player dies they can be resurrected by another player activating a rune stone (magical object at fixed location in a level)

**Levels**  
Electricity generator that needs fuel to run.
Junctions boxes that need to be repaired.
Electrical conveyors and doors need power to move / open
Machines and/or locks that need keys or can be hacked

### Description

Online game where players work together to move from the entrance to the exit of levels and sets of levels.
The players can meet in a staging area where they can create their characters and interact with other players, form teams and begin level sets.

A central server will provide the staging area and levels sets to the distributed game application - but the application will instantiate the level sets. This is so the game can be played without a server (i.e. the application itself is the game server with a certain instance hosting an actual game instance)

The central server enables players to easily connect over the internet and receive game and level updates.

The application will have 'join game' and 'join server' options

### Important Objects

- Server
    - Connection list
    - LevelSet
    - Game
- Game
    - Level
    - MessageServer
- MessageServer
    - Recieves messages
- Client
    - Connection
    - MessageClient
- Connection
    - network ID
- Player
    - name (handle)
    - characters (list)
    - Connection
    - icon
- Character (inherited from MapObject?)
    - name
    - class
    - skills
    - bag
    - clothes
- Class (enum? Attribute modifier?)
    - name
    - skill modifiers
- MapObject (an item that can occupy a map location)
    - static? item like a wall
    - generator?
    - can be picked up or not (item?)
    - icon
- Map
    Description: Collection of MapObjects. Data from which graphics engine renders the game view.
    - matrix of references to MapObjects
- Item (inherits from MapObject?)
    Description: MapObject that can be held by character
    - Weight
    - Method: Use
- Weapon (inherits from Item)
    - Range
    - Damage
- Spell (inherits from Item)
- Clothing (inherits from Item)
- Level
    - LevelMap (inherited from Map)
- LevelSet
    - levels (list)
- Stage
    - StageMap (inherited from Map)
    - LevelSets (list)
    - Players (list)

### Interactions

- A player can have multiple characters
- A must specify a character to connect to or start a game
