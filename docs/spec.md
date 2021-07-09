## Specification

This file is like the analysis but it only contains agreed specifics.

Uncertainties should not live here but this is still a fluid document.

This document should represent this final vision for the game - not the actual state of the game.

Progress of development will be determined by ***stories*** & ***epics*** in the [workflow](workflow/README.md).

### Toolset

Unity and .Net

### Scenario

Please see [scenario.md](scenario.md)

### Requirements

|Application
|---
|Single and Multiplayer (online or LAN)
|All players in level at once
|Staging area leading to dungeon levels (sets)
|Simple small dungeon levels
|When a character leaves a level they cannot return. They are therefor in 'transit'.
|The level does not end until all players have left the level.
|If a player dies they can be resurrected by another player activating a rune stone (magical object at fixed location in a level)

|Levels
|---
|Electricity generator that needs fuel to run.
|Junctions boxes that need to be repaired.
|Electrical conveyors and doors need power to move / open
|Machines and/or locks that need keys or can be hacked

### Description
Online game where players work together to move from the entrance to the exit of levels and sets of levels.
The players can meet in a staging area where they can create their characters and interact with other players, form teams and begin level sets.
