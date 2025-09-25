using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MMORPG.Common.Tool
{
    public static class DataHelper
    {
        public static Vector3 ParseVector3(string str)
        {
            var floats = ParseFloats(str);
            if (floats.Length == 0)
            {
                return Vector3.Zero;
            }
            Debug.Assert(floats.Length == 3);
            Vector3 res;
            res.X = floats[0];
            res.Y = floats[1];
            res.Z = floats[2];
            return res;
        }

        public static float[] ParseFloats(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return Array.Empty<float>();

            str = str.Trim();

            if ((str.StartsWith("[") && str.EndsWith("]")) ||
                (str.StartsWith("(") && str.EndsWith(")")))
            {
                str = str[1..^1];
            }

            var parts = str.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var result = new List<float>();

            foreach (var p in parts)
            {
                if (float.TryParse(p.Trim(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float value))
                {
                    result.Add(value);
                }
                else
                {
                    Console.WriteLine($"[ParseFloats] '{p}' cannot conver to float. Input: '{str}'");
                }
            }

            return result.ToArray();
        }

        public static int[] ParseIntegers(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return Array.Empty<int>();

            str = str.Trim();
            if (str.StartsWith("[") && str.EndsWith("]"))
                str = str[1..^1];

            return str
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.Parse(s.Trim(), CultureInfo.InvariantCulture))
                .ToArray();
        }
    }
}