﻿using MMORPG.Common.Proto.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Tool
{
    public static class VectorHelper
    {
        public static readonly float Rad2Deg = 180 / (float)Math.PI;

        /// <summary>
        /// Direction vector to Euler angle
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static Vector3 ToEulerAngles(this Vector3 direction)
        {
            var num = MathF.Sqrt(
                (direction.X * direction.X + direction.Z * direction.Z) /
                (direction.X * direction.X + direction.Y * direction.Y + direction.Z * direction.Z)
            );
            var eulerAngles = new Vector3
            {
                // Anglex = arc cos(sqrt((x^2 + z^2) / (x^2 + y^2 + z^2)))
                X = MathF.Acos(num) * Rad2Deg
            };

            if (direction.Y > 0) eulerAngles.X = 360 - eulerAngles.X;

            // AngleY = arc tan(x/z)
            eulerAngles.Y = MathF.Atan2(direction.X, direction.Z) * Rad2Deg;
            if (eulerAngles.Y < 0) eulerAngles.Y += 180;
            if (direction.X < 0) eulerAngles.Y += 180;

            // AngleZ = 0
            eulerAngles.Z = 0;
            return eulerAngles;
        }

        public static Vector3 Normalized(this Vector3 value)
        {
            var magnitude = (float)Math.Sqrt((double)value.X * (double)value.X + 
                                             (double)value.Y * (double)value.Y + 
                                             (double)value.Z * (double)value.Z);
            if (magnitude > 1E-05f)
                return value / magnitude;
            return Vector3.Zero;
        }

        public static Vector2 ToVector2(this Vector3 vector)
        {
            return new Vector2(vector.X, vector.Z);
        }

        public static Vector2 RotateVector2(Vector2 v, float angle)
        {
            var radians = angle * ((float)Math.PI / 180f);

            var cos = (float)Math.Cos(radians);
            var sin = (float)Math.Sin(radians);
            var newX = v.X * cos - v.Y * sin;
            var newY = v.X * sin + v.Y * cos;

            return new Vector2(newX, newY);
        }

        public static Vector2 Normalized(this Vector2 value)
        {
            float magnitude = (float)Math.Sqrt(value.X * value.X + value.Y * value.Y);
            if (magnitude > 1E-05f)
                return value / magnitude;
            return Vector2.Zero;
        }

        public static Vector3 ToVector3(this Vector2 vector)
        {
            return new Vector3(vector.X, 0, vector.Y);
        }

        public static float ToEulerAngles(this Vector2 direction)
        {
            return (float)Math.Atan2(direction.X, direction.Y) * Rad2Deg;
        }

        public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDistanceDelta)
        {
            float toVectorX = target.X - current.X;
            float toVectorY = target.Y - current.Y;

            float sqDist = toVectorX * toVectorX + toVectorY * toVectorY;

            if (sqDist == 0 || (maxDistanceDelta >= 0 && sqDist <= maxDistanceDelta * maxDistanceDelta))
                return target;

            float dist = (float)Math.Sqrt(sqDist);

            return new Vector2(current.X + toVectorX / dist * maxDistanceDelta,
                current.Y + toVectorY / dist * maxDistanceDelta);
        }
    }
}
