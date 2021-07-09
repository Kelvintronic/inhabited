**DandeLion**

A Unity based interperetation of the 8-bit Atari game Dandy.

https://en.wikipedia.org/wiki/Dandy_(video_game)

The main point of difference is that this version implements online multiplayer.

To host the game you must set up port forwarding on your modem. The game currently uses port 10515.

The game is based on the NetGameExample from the RevenantX/LiteNetLib repository.

**Simplified Architecture**

Client and Server

These objects are instantiated as DoNotDestroyOnLoad and implement LiteNetLib for their network communication layer.

Levels

Levels are managed by the persistant LevelSet object. Both the client and server share a reference to this. Each level is a prefab containing two tilemaps. One tilemap contains the 'walls' only and the other contains all objects to be instantiated at game start.

When Host is clicked the server object starts, followed by the client. The server determines the starting level and sends a packet to all clients (initially only its local client) that then instantiate the level using the SetLevel method of the LevelSet object. When the level starts the object tilemap is hidden. The server requests the current level from LevelSet in the form of a MapArray. A MapArray is essentially a two dimensional array of enum object types. The Level object can provide a different MapArray each of the tilemaps held by that level. The server initally recieves the 'walls' MapArray and this is shared with the ServerObjectManager (SOM). This array then becomes the main MapArray that will soon contain references to the objects as well. The server next recieves the object MapArray and iterates through, creating a ServerWorldObject for each object type and adding these to the SOM. When an object is added the SOM also adds a reference to the object in the main MapArray. The MapArray is designed to hold the id of the individual objects and this is why they are added separately. During the next server tick all objects are serialised and sent to the clients. The clients then instantiates the object prefabs.

Objects

There are ServerWorldObjects and WorldObjects. These do not contain any Unity interfaces and are used only to store object data at the server and clients respectively. ServerWorldObject has an Update method that is called every tick. If the Update method (or another server method) cause a change to an object then it is marked for update and sent to the clients on the next tick.

ObjectViews

Each object has an equivalent ObjectView that is a Unity object derived from MonoBehaviour and are prefabs. If an object has no client interaction then they use GenericView which means that they only render a specified sprite. When an object update arrives at the client the IObjectView interface method Update is called with the new WorldObject version or if there is no instance of the ObjectView then one is instantiated.

The server can move objects around by simply changing their WorldObject position variable and setting the update flag. ObjectView prefabs have colliders but no rigidbodies. They cannot collide with each other and so can take up the same space if set to the same location. To avoid this happening the MapArray must be checked before attempting to move an object to a new location. If the cell is empty then it must be assigned as 'owned' by the object to be moved and then the movement can occurr. Objects are responsible for cleaning up the array when they are destroyed.

NPCs

NPCs are simply objects like any other. All of the AI is actioned when the ServerWorldObject's Update method is called. However the NPC prefabs have a sensor attached that can detect players. The sensors invoke a server callback to update that object's eyesOn variable - i.e. the player that the NPC is currently watching.

ObjectManagers

There is a ServerObjectManager and a ClientObjectManager. The server version contains only ServerWorldObjects and uses an interface to the Server to update client objects each tick. If an object is removed from the SOM then it's Destroy method is called and a destroy object packet is sent to the client. The client version contains WorldObjects and ObjectViews if a destroy object packet is recieved it destroys these.

Players

Players are prefabs. There are ClientPlayer and RemotePlayer prefabs. The client version is responsible for getting player input whereas the remote version simply displays where the other players are to that client player. 

The client player intance for each game instance is the true holder of that players position. Any collisions with objects occur only for that client and the remote player instances on the other clients reflect that.

Attacks

NPC attacks only occur at the host players instance. The code for deciding to attack is in the NPCView. When the decision is made to attack a local callback to the server is invoked.

Player attacks (firing bolts) also only occur at the host players instance. There are two bolt prefabs; ServerBolt and ClientBolt. When a player fires, the host player's client logic instantiates a ServerBolt whereas the non-hosting client logic instantiates a ClientBolt. The difference between the prefabs is that the server version has a collider and when it hits an object its script invokes a local callback to the server.









