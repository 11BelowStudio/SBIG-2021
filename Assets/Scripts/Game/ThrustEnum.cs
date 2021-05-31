using UnityEngine;

namespace Game
{
    public enum ThrustEnum
    {
        UP_THRUSTER,
        DOWN_THRUSTER,
        LEFT_THRUSTER,
        RIGHT_THRUSTER
    }

    public static class ThrustEnumUtilities
    {
        public static string ThrustEnumToString(ThrustEnum t)
        {
            switch (t)
            {
                case ThrustEnum.UP_THRUSTER:
                    return "up";
                case ThrustEnum.DOWN_THRUSTER:
                    return "down";
                case ThrustEnum.LEFT_THRUSTER:
                    return "left";
                case ThrustEnum.RIGHT_THRUSTER:
                    return "right";
            }
            return "idk either";
        }

        public static Vector2 ThrustEnumToVector2(ThrustEnum t)
        {
            switch (t)
            {
                case ThrustEnum.UP_THRUSTER:
                    return Vector2.up;
                case ThrustEnum.DOWN_THRUSTER:
                    return Vector2.down;
                case ThrustEnum.LEFT_THRUSTER:
                    return Vector2.left;
                case ThrustEnum.RIGHT_THRUSTER:
                    return Vector2.right;
            }
            return Vector2.zero;
        }
        
    }
}