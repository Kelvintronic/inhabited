using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine
{
    public struct WorldVector
    {
        public float x;
        public float y;

        public WorldVector(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static float Distance(WorldVector a, WorldVector b)
        {
            float x = MathFloat.Normalise(a.x - b.x);
            float y = MathFloat.Normalise(a.y - b.y);
            return Convert.ToSingle(Math.Sqrt(Convert.ToDouble(x * x) + Convert.ToDouble(y * y)));
        }

        public static float Dot(WorldVector vector1, WorldVector vector2)
        {
            return (vector1.x * vector2.x) + (vector1.x * vector2.x);
        }

        public WorldVector Round(int nDecimalPlaces)
        {
            if (nDecimalPlaces < 1)
                nDecimalPlaces = 1;
            if (nDecimalPlaces > 4)
                nDecimalPlaces = 4;

            float divisor = 0.00001f * (10 ^ (5 - nDecimalPlaces));
            return new WorldVector(MathFloat.Round(this.x, divisor),MathFloat.Round(this.y, divisor));
        }

        public bool IsClose(WorldVector compareVector,float margin)
        {
            if ((this.x < compareVector.x + margin && this.x > compareVector.x - margin) &&
               (this.y < compareVector.y + margin && this.y > compareVector.y - margin))
                return true;
            else
                return false;
        }

        public float LengthSquared()
        {
            return this.x * this.x + this.y * this.y;
        }

        public float Length()
        {
            return Convert.ToSingle(Math.Sqrt(this.x * this.x + this.y * this.y));
        }

        // return WorldVector of length = 1;
        public WorldVector Normalize()
        {
            float length = Length();
            return new WorldVector(this.x / length, this.y/length);
        }

        public static WorldVector Lerp(WorldVector firstVector, WorldVector secondVector, float by)
        {
            WorldVector vector;
            vector.x = MathFloat.Lerp(firstVector.x, secondVector.x, by);
            vector.y = MathFloat.Lerp(firstVector.y, secondVector.y, by);
            return vector;
        }

        public static WorldVector operator +(WorldVector a, WorldVector b)
        {
            WorldVector vector;
            vector.x = a.x + b.x;
            vector.y = a.y + b.y;
            return vector;
        }
        public static WorldVector operator -(WorldVector a, WorldVector b)
        {
            WorldVector vector;
            vector.x = a.x - b.x;
            vector.y = a.y - b.y;
            return vector;
        }
        public static WorldVector operator *(float d, WorldVector a)
        {
            WorldVector vector;
            vector.x = d * a.x;
            vector.y = d * a.y;
            return vector;
        }
        public static WorldVector operator *(WorldVector a, float d)
        {
            WorldVector vector;
            vector.x = d * a.x;
            vector.y = d * a.y;
            return vector;
        }
        public static WorldVector operator *(WorldVector a, WorldVector b)
        {
            WorldVector vector;
            vector.x = b.x * a.x;
            vector.y = b.y * a.y;
            return vector;
        }
        public static WorldVector operator /(WorldVector a, float d)
        {
            WorldVector vector;
            vector.x = a.x / d;
            vector.y = a.y / d;
            return vector;
        }

        public static WorldVector operator /(WorldVector a, WorldVector b)
        {
            WorldVector vector;
            vector.x = a.x / b.x;
            vector.y = a.y / b.y;
            return vector;
        }

        public static bool operator ==(WorldVector lhs, WorldVector rhs)
        {
            if (lhs.x == rhs.x && lhs.y == rhs.y)
                return true;
            return false;
        }
        public static bool operator !=(WorldVector lhs, WorldVector rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this == (WorldVector)obj;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode()^y.GetHashCode();
        }

        public static WorldVector Zero { get { return new WorldVector { x = 0, y = 0 }; } }

    }

    public struct VectorInt
    {
        public int x;
        public int y;

        public VectorInt(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static bool operator ==(VectorInt lhs, VectorInt rhs)
        {
            if (lhs.x == rhs.x && lhs.y == rhs.y)
                return true;
            return false;
        }
        public static bool operator !=(VectorInt lhs, VectorInt rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this == (VectorInt)obj;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }
    }

    public static class MathFloat
    {
        public const float Deg2Rad = 0.0174532924F;
        public const float Rad2Deg = 57.29578F;

        public static float Sin(float f)
        {
            return Convert.ToSingle(Math.Sin(Convert.ToDouble(f)));
        }
        public static float Cos(float f)
        {
            return Convert.ToSingle(Math.Cos(Convert.ToDouble(f)));
        }

        public static float Normalise(float f)
        {
            if (f < 0)
                return 0 - f;
            return f;
        }

        public static float Lerp(float firstFloat, float secondFloat, float by)
        {
            //return firstFloat * (1 - by) + secondFloat * by;
            return firstFloat + ((secondFloat - firstFloat) * by);
        }

        static Random random = new Random();
        public static float Random(float minimum, float maximum)
        {
            return Convert.ToSingle(random.NextDouble() * (maximum - minimum) + minimum);
        }

        public static float Round(float number, float divisor)
        {
            int integer= (int)Math.Floor(number);
            float remainder = number - integer;
            int count = (int)Math.Floor(remainder / divisor);
            return (float)integer + divisor * count;
        }
    }

    /// implements improved Perlin noise in 2D. 
    /// Transcribed from http://www.siafoo.net/snippet/144?nolinenos#perlin2003
    /// </summary>
    public static class Noise2d
    {
        private static Random _random = new Random();
        private static int[] _permutation;

        private static WorldVector[] _gradients;

        static Noise2d()
        {
            CalculatePermutation(out _permutation);
            CalculateGradients(out _gradients);
        }

        private static void CalculatePermutation(out int[] p)
        {
            p = Enumerable.Range(0, 256).ToArray();

            /// shuffle the array
            for (var i = 0; i < p.Length; i++)
            {
                var source = _random.Next(p.Length);

                var t = p[i];
                p[i] = p[source];
                p[source] = t;
            }
        }

        /// <summary>
        /// generate a new permutation.
        /// </summary>
        public static void Reseed()
        {
            CalculatePermutation(out _permutation);
        }

        private static void CalculateGradients(out WorldVector[] grad)
        {
            grad = new WorldVector[256];

            for (var i = 0; i < grad.Length; i++)
            {
                WorldVector gradient;

                do
                {
                    gradient = new WorldVector((float)(_random.NextDouble() * 2 - 1), (float)(_random.NextDouble() * 2 - 1));
                }
                while (gradient.LengthSquared() >= 1);

                gradient.Normalize();

                grad[i] = gradient;
            }

        }

        private static float Drop(float t)
        {
            t = Math.Abs(t);
            return 1f - t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static float Q(float u, float v)
        {
            return Drop(u) * Drop(v);
        }

        public static float Noise(float x, float y)
        {
            var cell = new WorldVector((float)Math.Floor(x), (float)Math.Floor(y));

            var total = 0f;

            var corners = new[] { new WorldVector(0, 0), new WorldVector(0, 1), new WorldVector(1, 0), new WorldVector(1, 1) };

            foreach (var n in corners)
            {
                var ij = cell + n;
                var uv = new WorldVector(x - ij.x, y - ij.y);

                var index = _permutation[(int)ij.x % _permutation.Length];
                index = _permutation[(index + (int)ij.y) % _permutation.Length];

                var grad = _gradients[index % _gradients.Length];

                total += Q(uv.x, uv.y) * WorldVector.Dot(grad, uv);
            }

            return Math.Max(Math.Min(total, 1f), -1f);
        }

    }

}
