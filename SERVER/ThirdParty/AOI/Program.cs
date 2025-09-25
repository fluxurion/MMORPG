using System;
using System.Numerics;

namespace AOI
{
    class Program
    {
        static void Main(string[] args)
        {
            var zone = new AoiZone(.001f, .001f);
            var area = new Vector2(1, 1);

            // Add 500 players.

            for (var i = 1; i <= 6; i++)
            {
                for (var j = 1; j <= 6; j++)
                {
                    zone.Enter((i - 1) * 6 + j, i, j);
                }
            }

            // Test move.
            // while (true)
            // {
            //     Console.WriteLine("1");
            //     zone.Refresh(new Random().Next(0, 50000), new Random().Next(0, 50000), new Random().Next(0, 50000), area);
            //     Console.WriteLine("2");
            // }

            // Refresh the information with key 3.

            zone.Refresh(3, area, out var enters);

            Console.WriteLine("---------------List of players that have joined the player scope--------------");
    
            foreach (var aoiKey in enters)
            {
                var findEntity = zone[aoiKey];
                Console.WriteLine($"entity:{aoiKey} X:{findEntity.X.Value} Y:{findEntity.Y.Value}");
            }
    
            // 更新key为3的坐标。
    
            var entity = zone.Refresh(3, 1, 3, area, out enters);


            Console.WriteLine("---------------List of players who have left player range--------------");
    
            foreach (var aoiKey in entity.Leave)
            {
                var findEntity = zone[aoiKey];
                Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");
            }

            Console.WriteLine("---------------List of players entering the player range--------------");

            foreach (var aoiKey in entity.Enter)
            {
                var findEntity = zone[aoiKey];
                Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");
            }




            Console.WriteLine("---------------key is the new player list within the player range after 3 moves--------------");
    
            foreach (var aoiKey in entity.ViewEntity)
            {
                var findEntity = zone[aoiKey];
                Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");
            }

            // 离开当前AOI

            zone.Exit(50);
        }
    }
}