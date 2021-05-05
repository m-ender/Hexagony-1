using static Hexagony.Direction;

namespace Hexagony
{
    public enum Direction
    {
        East,
        SouthEast,
        SouthWest,
        West,
        NorthWest,
        NorthEast,
    }

    public static class DirectionExtensions
    {
        private static Direction[] reflectAtSlash = new[] { NorthWest, West, SouthWest, SouthEast, East, NorthEast };
        private static Direction[] reflectAtBackslash = new[] { SouthWest, SouthEast, East, NorthEast, NorthWest, West };
        private static Direction[] reflectAtUnderscore = new[] { East, NorthEast, NorthWest, West, SouthWest, SouthEast };
        private static Direction[] reflectAtPipe = new[] { West, SouthWest, SouthEast, East, NorthEast, NorthWest };
        private static Direction[] reflectAtLessThanIfPositive = new[] { SouthEast, NorthWest, West, East, West, SouthWest };
        private static Direction[] reflectAtLessThanIfNegative = new[] { NorthEast, NorthWest, West, East, West, SouthWest };
        private static Direction[] reflectAtGreaterThanIfPositive = new[] { West, East, NorthEast, NorthWest, SouthEast, East };
        private static Direction[] reflectAtGreaterThanIfNegative = new[] { West, East, NorthEast, SouthWest, SouthEast, East };

        private static PointAxial[] unitVector = new PointAxial[] { new(1, 0), new(0, 1), new(-1, 1), new(-1, 0), new(0, -1), new(1, -1) };
        private static string[] toString = new string[] { "E", "SE", "SW", "W", "NW", "NE" };

        public static Direction ReflectAtSlash(this Direction dir) => reflectAtSlash[(int)dir];
        public static Direction ReflectAtBackslash(this Direction dir) => reflectAtBackslash[(int)dir];
        public static Direction ReflectAtUnderscore(this Direction dir) => reflectAtUnderscore[(int)dir];
        public static Direction ReflectAtPipe(this Direction dir) => reflectAtPipe[(int)dir];
        public static Direction ReflectAtLessThan(this Direction dir, bool positive) 
            => (positive ? reflectAtLessThanIfPositive : reflectAtLessThanIfNegative)[(int)dir];
        public static Direction ReflectAtGreaterThan(this Direction dir, bool positive)
            => (positive ? reflectAtGreaterThanIfPositive : reflectAtGreaterThanIfNegative)[(int)dir];
        
        public static PointAxial Vector(this Direction dir) => unitVector[(int)dir];

        public static string ToString(this Direction dir) => toString[(int)dir];
    }
}
