using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameEngine
{

    public class MapCave
    {
        public int id;
        public int size;
        public int x;
        public int y;
        public List<Vector2Int> borderCells;

        public MapCave(int id, int size, int x, int y)
        {
            this.x = x;
            this.y = y;
            this.size = size;
            this.id = id;
            borderCells = new List<Vector2Int>();
        }
    }

    class MapHelpers
    {
        // A recursive function to replace
        // previous color 'prevC' at '(x, y)'
        // and all surrounding pixels of (x, y)
        // with new color 'newC' and
        static int floodFillUtil(int[,] screen,
                                int x, int y,
                                int prevC, int newC,
                                int count)
        {
            // Base cases
            if (x < 0 || x >= screen.GetUpperBound(0) ||
                y < 0 || y >= screen.GetUpperBound(1))
                return count;
            if (screen[x, y] != prevC)
                return count;

            // Replace the color at (x, y)
            screen[x, y] = newC;
            count++;

            // Recur for north, east, south and west
            count = floodFillUtil(screen, x + 1, y, prevC, newC, count);
            count = floodFillUtil(screen, x - 1, y, prevC, newC, count);
            count = floodFillUtil(screen, x, y + 1, prevC, newC, count);
            count = floodFillUtil(screen, x, y - 1, prevC, newC, count);

            return count;
        }

        // It mainly finds the previous color
        // on (x, y) and calls floodFillUtil()
        public static int FloodFill(int[,] screen, int x,
                            int y, int newC)
        {
            int prevC = screen[x, y];
            return floodFillUtil(screen, x, y, prevC, newC, 0);
        }

        /// <summary>
        /// Takes map array and finds all distinct caves within and fills them with a different number
        /// Cave walls are 1 and map must be bounded by these also
        /// Empty cells are 0
        /// </summary>
        /// <param name="map"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        public static List<MapCave> FillCaves(int[,] map)
        {
            var caves = new List<MapCave>();

            // iterate through all cells and find empty cells in turn
            // and flood fill each with the current caveCode
            // after each flood fill increment caveCode
            int caveCode = 2;
            for (int y = 0; y < map.GetUpperBound(1); y++)
            {
                for (int x = 0; x < map.GetUpperBound(0); x++)
                {
                    if (map[x, y] == 0)
                    {
                        Debug.Log("Cave filled with" + caveCode);
                        int size = FloodFill(map, x, y, caveCode);
                        caves.Add(new MapCave(caveCode, size, x, y));

                        caveCode++;
                    }
                }
            }
            return caves;
        }

        public static void TraceCaveWall(int[,] map, MapCave cave)
        {
            // starting point - we know this is on the border
            Vector2Int vector = new Vector2Int(cave.x, cave.y);

            //Set a move limit in case the end condition is never found
            for (int moveLimit = 0; moveLimit < 1000; moveLimit++)
            {
                var returnVector = FindNextClockwiseStep(map, vector);

                // no patern found condition
                if (returnVector == vector)
                    break;

                // trace border until we get back to where we started
                if (returnVector.x == cave.x && returnVector.y == cave.y)
                    break;

                cave.borderCells.Add(returnVector);

                vector = returnVector;
            }

        }

        /*  private static int[,] none =    { { 0, 0 },
                                    { 0, 0 } }; this one is arbitary*/
        private static int[,] one =     { { 0, 0 },
                                          { 0, 1 } };
        private static int[,] two =     { { 0, 1 },
                                          { 0, 1 } };
        /*private static int[,] three =   { { 0, 1 },
                                          { 1, 1 } };   This one has no solution */
        /*private static int[,] four =    { { 0, 0 },
                                          { 1, 0 } };   This one won't work for clockwise*/
        private static int[,] five =    { { 0, 1 },
                                          { 0, 0 } };
 
        // Clockwise logic!
        public static Vector2Int FindNextClockwiseStep(int[,] map, Vector2Int testPoint)
        {
            int result = FindMatch(map, testPoint, one);
            Debug.Log("FindMatch 1 complete");
            if(result!=-1)
            {
                switch (result)
                {
                    // one @ 0 = go y-1 
                    // one @ 1 = go x-1
                    // one @ 2 = go y+1
                    // one @ 3 = go x+1

                    case 0:
                        Debug.Log("patern 1 case 0 found");
                        return new Vector2Int(testPoint.x, testPoint.y - 1);
                    case 1:
                        Debug.Log("patern 1 case 1 found");
                        return new Vector2Int(testPoint.x-1, testPoint.y);
                    case 2:
                        Debug.Log("patern 1 case 2 found");
                        return new Vector2Int(testPoint.x, testPoint.y + 1);
                    case 3:
                        Debug.Log("patern 1 case 3 found");
                        return new Vector2Int(testPoint.x+1, testPoint.y);
                }
            }
            result = FindMatch(map, testPoint, two);
            Debug.Log("FindMatch 2 complete");
            if (result != -1)
            {
                switch (result)
                {
                    // two @ 0 = go y-1
                    // two @ 1 = go x-1
                    // two @ 2 = go y+1
                    // two @ 3 = go x+1

                    case 0:
                        Debug.Log("patern 2 case 0 found");
                        return new Vector2Int(testPoint.x, testPoint.y - 1);
                    case 1:
                        Debug.Log("patern 2 case 1 found");
                        return new Vector2Int(testPoint.x - 1, testPoint.y);
                    case 2:
                        Debug.Log("patern 2 case 2 found");
                        return new Vector2Int(testPoint.x, testPoint.y + 1);
                    case 3:
                        Debug.Log("patern 2 case 3 found");
                        return new Vector2Int(testPoint.x + 1, testPoint.y);
                }
            }
            result = FindMatch(map, testPoint, five);
            Debug.Log("FindMatch 3 complete");
            if (result != -1)
            {
                switch (result)
                {
                    // three @ 0 = go y-1 and x+1
                    // three @ 1 = go x-1 and y-1
                    // three @ 2 = go x-1 and y+1 
                    // three @ 3 = go x+1 and y+1

                    case 0:
                        Debug.Log("patern 5 case 0 found");
                        return new Vector2Int(testPoint.x+1, testPoint.y - 1);
                    case 1:
                        Debug.Log("patern 5 case 1 found");
                        return new Vector2Int(testPoint.x - 1, testPoint.y-1);
                    case 2:
                        Debug.Log("patern 5 case 2 found");
                        return new Vector2Int(testPoint.x-1, testPoint.y + 1);
                    case 3:
                        Debug.Log("patern 5 case 3 found");
                        return new Vector2Int(testPoint.x + 1, testPoint.y+1);
                }
            }
            
            // return the testPoint unchanged
            // !!! Caller must check for this condition !!!!
            return new Vector2Int(testPoint.x,testPoint.y);
        }

/*        private static int[,] one =     { { 0, 0 },
                                          { 0, 1 } };
        private static int[,] two =     { { 0, 1 },
                                          { 0, 1 } };
        private static int[,] five =    { { 0, 1 },
                                          { 0, 0 } };*/

        private static Vector2Int[,] RotateMatrix = {   { new Vector2Int(0, 0), new Vector2Int(1, 0),
                                                          new Vector2Int(0, -1), new Vector2Int(1, -1)},
                                                        { new Vector2Int(0, 0), new Vector2Int(0, -1),
                                                           new Vector2Int(-1, 0),new Vector2Int(-1, -1)},

                                                        { new Vector2Int(0, 0), new Vector2Int(-1, 0),
                                                          new Vector2Int(0, 1), new Vector2Int(-1, 1)},
                                                        { new Vector2Int(0, 0), new Vector2Int(0, 1),
                                                          new Vector2Int(1, 0), new Vector2Int(1, 1)} };
        // one @ 0 = go y-1 
        // one @ 1 = go x-1
        // one @ 2 = go y+1
        // one @ 3 = go x+1

        // two @ 0 = go y-1
        // two @ 1 = go x-1
        // two @ 2 = go y+1
        // two @ 3 = go x+1

        // three @ 0 = go y-1 and x+1
        // three @ 1 = go x-1 and y-1
        // three @ 2 = go x-1 and y+1 
        // three @ 3 = go x+1 and y+1

        /// <summary>
        /// checks for the 2x2 matrix patern around the specified point
        /// i.e. each four quadrants are checked as if they were rotated
        /// to the bottom right corner of a 3x3 grid.
        /// the return value corresponds to the number of times the matrix
        /// was rotated before finding a match. If no match is found -1 is 
        /// returned.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="testPoint"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>

        private static int FindMatch(int[,] map, Vector2Int testPoint, int[,] matrix)
        {
            for (int rotate = 0; rotate < 4; rotate++)
            {
                int matrixIndex = 0;
                for(int x = 0; x<2; x++)
                {
                    for(int y=0; y<2; y++)
                    {
                        if(map[testPoint.x+ RotateMatrix[rotate,matrixIndex].x,testPoint.y+ RotateMatrix[rotate,matrixIndex].y]== matrix[x,y])
                            matrixIndex++;
                    }
                }

                if (matrixIndex == 4)
                    return rotate;
            }
            return -1;
        }

        /// <summary>
        /// move given point away from walls by margin
        /// </summary>
        /// <param name="map"></param>
        /// <param name="point"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        public static Vector2Int BufferPoint(int[,] map, Vector2Int point, int wallCode, int margin)
        {
            // Base cases
            if (point.x < 0 || point.x >= map.GetUpperBound(0) ||
                point.y < 0 || point.y >= map.GetUpperBound(1))
                return point;
            if (map[point.x, point.y] == wallCode)
                return point;

            // check all squares around point and adjust movement vector based on these
            // if square contains wall code set vector to move away from wall
            var vector = new Vector2Int();
            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    if (map[point.x + x, point.y + y] == wallCode)
                    {
                        vector.x += 0 - x;
                        vector.y += 0 - y;
                    }
                }
            }

            // normalise the vector
            if (vector.x > 1)
                vector.x = 1;
            if (vector.x < -1)
                vector.x = -1;
            if (vector.y > 1)
                vector.y = 1;
            if (vector.y < -1)
                vector.y = -1;

            // apply the vector
            return new Vector2Int(point.x, point.y) + vector;
        }

    }
}
