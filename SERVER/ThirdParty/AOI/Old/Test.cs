using System;
using System.Numerics;

namespace AOI.Old
{
    public static class Test
    {
        public static void Run()
        {
            var aoi = new AoiComponent();
            
            var role1 = aoi.Enter(1, 12, 8);
            
            Console.WriteLine($"Player ID:{role1.Id}");
            
            var role2 = aoi.Enter(2, 12, 8);
            
            Console.WriteLine($"Player 2 ID:{role2.Id}");
            
            aoi.Update(2, new Vector2(1, 1), 13, 8);  // Player two moves

            Console.WriteLine($"Player 2's surrounding list");
            
            foreach (var aoiNode in role2.AoiInfo.MovesSet)
            {
                Console.WriteLine(aoi.GetNode(aoiNode).Position);
            }
            
            Console.WriteLine($"Player 2 enters the list");
            
            foreach (var aoiNode in role2.AoiInfo.EntersSet)
            {
                Console.WriteLine(aoi.GetNode(aoiNode).Position);
            }
            
            Console.WriteLine($"Player 2 leaves the list");
            
            foreach (var aoiNode in role2.AoiInfo.LeavesSet)
            {
                Console.WriteLine(aoi.GetNode(aoiNode).Position);
            }
            
            Console.WriteLine($"Player 2's move list");
            
            foreach (var aoiNode in role2.AoiInfo.MoveOnlySet)
            {
                Console.WriteLine(aoi.GetNode(aoiNode).Position);
            }
        }
    }
}