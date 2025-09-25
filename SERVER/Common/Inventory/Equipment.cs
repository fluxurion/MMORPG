using System.Collections;
using System.Collections.Generic;

namespace MMORPG.Common.Inventory
{
    /// <summary>
    /// equipment
    /// </summary>
    public class Equipment : Item
    {
        public Equipment(ItemDefine define, int amount = 1, int slotId = 0) : base(define, amount, slotId)
        {
        }
    }
}