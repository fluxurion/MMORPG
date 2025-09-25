using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Security.Principal;

namespace Aoi
{
    /// Optimized Nine-Grid Aoi system

    /// Optimization solutions for frequent boundary crossings:
    /// | |c| | |
    /// | | |b| | |
    ///   | | |a| |
    ///     | | | |
    /// If we move from a to b, we do not immediately remove the 5 grids below and to the right of a (the retention basis is grid distance <= 1).
    /// At this time, if b moves to c again, the 5 grids with a distance > 1 are removed. In this way, the worst case is 16 grids, but the problem of broadcast entry and exit caused by frequent cross-border is basically avoided.



    internal class Program
    {
        static void Main(string[] args)
        {

            var zone = new AoiWord(20);
            var area = new Vector2(1, 1);

            Console.WriteLine($"{(-1) % 20 + 20}");

            Dictionary<int, AoiWord.AoiEntity> dir = new();

            // add player
            int count = 3;
            for (var i = 1; i <= count; i++)
            {
                for (var j = 1; j <= count; j++)
                {
                    dir[(i - 1) * count + j] = zone.Enter((i - 1) * count + j, i, j);
                    Console.WriteLine($"---------------{(i - 1) * count + j}--------------");

                    zone.ScanFollowerList(dir[(i - 1) * count + j], aoiKey =>
                    {
                        //var findEntity = zone[aoiKey];
                        //Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");

                        int x = aoiKey / count + 1;
                        int y = aoiKey % count;
                        if (y == 0)
                        {
                            y = count;
                            x--;
                        }

                        Console.WriteLine($"entity:{aoiKey} X:{x}, Y:{y}");
                    });
                }
            }

            for (var i = 1; i <= count; i++)
            {
                for (var j = 1; j <= count; j++)
                {
                    zone.ScanFollowingList(dir[(i - 1) * count + j], aoiKey =>
                    {
                        //var findEntity = zone[aoiKey];
                        //Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");

                        int x = aoiKey / count + 1;
                        int y = aoiKey % count;
                        if (y == 0)
                        {
                            y = count;
                            x--;
                        }
                        Console.WriteLine($"entity:{aoiKey} X:{x}, Y:{y}");
                    });
                    Console.WriteLine($"---------------{(i - 1) * count + j}--------------");
                }
            }

            {
                //Console.WriteLine($"---------------离开--------------");
                //zone.Leave(dir[1]);
                //foreach (var aoiKey in leaveList)
                //{
                //    //var findEntity = zone[aoiKey];
                //    //Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");

                //    int x = aoiKey / count + 1;
                //    int y = aoiKey % count;
                //    if (y == 0)
                //    {
                //        y = count;
                //        x--;
                //    }
                //    Console.WriteLine($"entity:{aoiKey} X:{x}, Y:{y}");
                //}
            }
            // Test move.
            // while (true)
            // {
            //     Console.WriteLine("1");
            //     zone.Refresh(new Random().Next(0, 50000), new Random().Next(0, 50000), new Random().Next(0, 50000), area);
            //     Console.WriteLine("2");
            // }



            //// Refresh the information with key 3.

            //zone.Refresh(3, area, out var enters);

            //Console.WriteLine("---------------List of players that have joined the player scope--------------");

            //foreach (var aoiKey in enters)
            //{
            //    var findEntity = zone[aoiKey];
            //    Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");
            //}

            //// Update the coordinates of key 3.

            //var entity = zone.Refresh(3, 1, 3, area, out enters);


            //Console.WriteLine("---------------List of players who have left player range--------------");

            //foreach (var aoiKey in entity.Leave)
            //{
            //    var findEntity = zone[aoiKey];
            //    Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");
            //}

            //Console.WriteLine("---------------List of players entering the player range--------------");

            //foreach (var aoiKey in entity.Enter)
            //{
            //    var findEntity = zone[aoiKey];
            //    Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");
            //}




            //Console.WriteLine("---------------key为3移动后玩家范围内的新玩家列表--------------");

            //foreach (var aoiKey in entity.ViewEntity)
            //{
            //    var findEntity = zone[aoiKey];
            //    Console.WriteLine($"X:{findEntity.X.Value} Y:{findEntity.Y.Value}");
            //}

            //// 离开当前AOI

            //zone.Exit(50);
        }
    }
}
