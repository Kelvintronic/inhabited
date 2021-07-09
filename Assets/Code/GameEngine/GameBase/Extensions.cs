using LiteNetLib.Utils;

namespace GameEngine
{
    public static class Extensions
    {      
        public static void Put(this NetDataWriter writer, WorldVector vector)
        {
            writer.Put(vector.x);
            writer.Put(vector.y);
        }

        public static WorldVector GetWorldVector(this NetDataReader reader)
        {
            return new WorldVector
            {
                x = reader.GetFloat(),
                y = reader.GetFloat()
            };
        }
    }
}