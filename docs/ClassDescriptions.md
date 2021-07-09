This document will attempt to explain how the NetGameEngine works in terms of its classes and their interactions

####Unity classes####

**UnityServerLogic**

Is derived from MonoBehaviour and assigned to a game object that exists at startup. It does not get destroyed until the game closes. It is simply a wrapper class for GameEngine:ServerLogic. It is primary job is to pass the Awake and Update unity messages to be passed on to ServerLogic.

**UnityClientLogic**

Is derived from MonoBehaviour and assigned to a game object that exists at startup. It does not get destroyed until the game closes. It is a wrapper class for GameEngine:ClientLogic.

- Its primary purpose is to implement GameEngine:IGameView so that GameEngine can instantiate ClientPlayerView and RemotePlayerView. 
- It passes the Awake and Update Unity messages on to ClientLogic.
- It recieves messages from GameEngine for the UI to display.
- It holds the dynamic sprites for assignment to any ClientPlayerView or RemotePlayerView when instantiated.

**ClientPlayerView**

Is derived from MonoBehaviour and assigned to a game object prefab that is instantiated on request by UnityClientLogic.

Properties:
GameEngine:ClientPlayer \_player  
    Holds all game related player data
Camera \_mainCamera_  
    Allows the mouse position to be captured
    
Purpose:  
- Render the player sprites and captures user input.
- Transform the player position based on the \_player property  

**RemotePlayerView**

Is derived from MonoBehaviour and assigned to a game object prefab that is instantiated on request by UnityClientLogic.

GameEngine:ClientPlayer \_player  
    Holds all game related player data

Purpose:  
- Render the player sprites
- Transform the player position based on the \_player property  

####GameEngine Classes####

**LogicTimer**

Properties:
const float FramesPerSecond  
    The refresh rate of the game
const float FixedDelta  
    The time in seconds of each period

Purpose:  
Instantiated with an action delegate method to be called at a certain refresh rate.

Note this does not mean that the delegate will get called at regular intervals!

Both the server and client logic use this timer. This timer has an update method that is called whenever the respective logic is updated (by Unity). The timer calls the delegate multiple times equal to the time elapsed since the last update divided by FixedDelta property. If Unity is set to 30 frames per second AND the timer is also set to this then the methods should be in sync. If however, Unity is set to a slower refresh rate then the refresh calls will be out of sync.

The game can be paused and resumed by stopping and starting the Server timer.

**ServerLogic**

Implements LiteNetLib:IListener

Primary Properties:  
NetManager \_netManager
    LiteNetLib class, initiated with the parent class to provide network connection and messaging
ServerPlayerManager \_playerManager  
    Holds all player data in terms of ServerPlayer (primarily network info)
ServerState \_serverState  
    Holds all game and player game data (used to update clients)

Purpose:  
Maintain the game state
Recieve input from clients
Send game state to all clients periodically

- UnityServerLogic passes on the Update message which initiates a poll of the network messages then calls the timer update.
- Receives messages from clients, primarily: JoinRequests and PlayerInput
- Maintains a rolling tick value for sinchronisation of messages


**ClientLogic**

Implements LiteNetLib:IListener

Purpose:  
Maintain local game state
Maintain local player views
Send user input messages
Recieves server messages
Passes game state on to \_playerManager for delegation

**ClientPlayer**

Applies server state when passed to it by ClientPlayerManager
Applies user input
Uses \_clientLogic to send user input packets

**RemotePlayer**

Applies server state when passed to it by ClientPlayerManager


#### Game Packets ####

**GamePackets.cs**

This file contains all of the packets used in network communication

