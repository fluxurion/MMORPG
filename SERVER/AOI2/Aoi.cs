using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Aoi
{
    /// <summary>
    /// AOI system based on the Nine-square grid
    /// Not thread safe
    /// </summary>
    public class AoiWord
    {
        public struct Vector2Int
        {
            public int X, Y;
        }

        public class AoiEntity
        {
            public int EntityId;
            public UInt64 ZoneKey;
            // Areas to be cleaned with a spacing of 2
            public HashSet<UInt64> PendingZoneKeySet = new();
            // Pay special attention to the target
            //public HashSet<int> SpecialFollowingSet = new();
        }

        public class AoiZone
        {
            // All entities in the current area
            public HashSet<int> EntitySet = new();

            // Pay attention to all entities in the region in Pending, that is, which entities have the current region in Pending
            public HashSet<int> PendingEntitySet = new();

            // The entity enters the current zone
            public void Enter(AoiEntity aoiEntity)
            {
                EntitySet.Add(aoiEntity.EntityId);
            }

            // The entity leaves the current zone
            public void Leave(AoiEntity aoiEntity)
            {
                EntitySet.Remove(aoiEntity.EntityId);
            }

        }

        private Dictionary<UInt64, AoiZone> _zoneDict = new();
        public Dictionary<int, AoiEntity> _entittDict = new();
        private int _zoneSize;

        public AoiWord(int zoneSize)
        {
            _zoneSize = zoneSize;
        }

        public AoiEntity Enter(int entityId, float x, float y)
        {
            Debug.Assert(!_entittDict.ContainsKey(entityId));

            PointToZonePoint(x, y, out var outX, out var outY);
            ZonePointToZoneKey(outX, outY, out var zoneKey);
            var aoiEntity = new AoiEntity()
            {
                EntityId = entityId,
                ZoneKey = zoneKey,
            };
            _entittDict.Add(entityId, aoiEntity);

            var zone = CreateZone(zoneKey);
            zone.Enter(aoiEntity);
            return aoiEntity;
        }

        public void Leave(AoiEntity aoiEntity)
        {
            var zone = _zoneDict[aoiEntity.ZoneKey];
            ZoneKeyToZonePoint(aoiEntity.ZoneKey, out var x, out var y);

            zone.Leave(aoiEntity);
            foreach (var pendingZoneKey in aoiEntity.PendingZoneKeySet)
            {
                var pendingZone = _zoneDict[pendingZoneKey];
                pendingZone.PendingEntitySet.Remove(aoiEntity.EntityId);
            }

            _entittDict.Remove(aoiEntity.EntityId);
        }

        /// <summary>
        /// Returns whether the boundary is crossed
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="newPos"></param>
        /// <param name="enterFollowingList">Returns a list of entities that have entered the current entity's view range after refresh.</param>
        /// <param name="leaveFollowingList">Returns a list of entities that have left the current entity's view range since the refresh.</param>
        /// <param name="enterFollowerList">Returns a list of entities whose line of sight the current entity will enter after refreshing</param>
        /// <param name="leaveFollowerList">Returns a list of entities whose sight distance the current entity will leave after refreshing</param>
        /// <returns></returns>
        public bool Refresh(AoiEntity aoiEntity, float x, float y, 
            Action<int> enterFollowingCallback, Action<int> leaveFollowingCallback, 
            Action<int> enterFollowerCallback, Action<int> leaveFollowerCallback)
        {
            var oldZoneKey = aoiEntity.ZoneKey;
                ZoneKeyToZonePoint(aoiEntity.ZoneKey, out var minX, out var minY);
            int maxX = minX + _zoneSize;
            int maxY = minY + _zoneSize;

            if (x < minX || y < minY ||
                x >= maxX || y >= maxY)
            {
                PointToZonePoint(x, y, out var newZoneX, out var newZoneY);

                //Console.WriteLine($"entity{aoiEntity.EntityId}PendingZone：");
                //foreach (var pendingZoneKey in aoiEntity.PendingZoneKeySet)
                //{
                //    ZoneKeyToZonePoint(pendingZoneKey, out var pendingZoneX, out var pendingZoneY);
                //    Console.Write($"[{pendingZoneX}, {pendingZoneY}]，");
                //}
                //Console.WriteLine();
                //Console.WriteLine();
                //Console.WriteLine($"entity{aoiEntity.EntityId}moving across borders：[{minX}, {minY}] -> [{newZoneX}, {newZoneY}]");

                ZonePointToZoneKey(newZoneX, newZoneY, out var newZoneKey);
                ZoneKeyToZonePoint(oldZoneKey, out var oldZoneX, out var oldZoneY);
                var oldZone = _zoneDict[oldZoneKey];
                var newZone = CreateZone(newZoneKey);

                // I might enter some entity's PendingZone, or even my own PendingZone
                foreach (var pendingEntityId in newZone.PendingEntitySet)
                {
                    if (pendingEntityId == aoiEntity.EntityId) continue;
                    var pendingEntity = _entittDict[pendingEntityId];
                    // But I may also have been watched by this entity.
                    if (IsFollowing(pendingEntity, aoiEntity)) continue;
                    enterFollowerCallback(pendingEntityId);
                }

                oldZone.Leave(aoiEntity);
                newZone.Enter(aoiEntity);
                aoiEntity.ZoneKey = newZoneKey;

                // I might be out of sight of some entity that is following me via the PendingZone
                foreach (var pendingEntityId in oldZone.PendingEntitySet)
                {
                    var pendingEntity = _entittDict[pendingEntityId];
                    // My new location may still be within the entity's area of ​​interest.
                    if (IsFollowing(pendingEntity, aoiEntity)) continue;
                    leaveFollowerCallback(pendingEntityId);
                }

                // Crossing the boundary, determine the entities that leave the field of view and enter the field of view based on the new nine-square grid
                var oldViewZoneArray = GetViewZoneArray(oldZoneX, oldZoneY);
                var newViewZoneArray = GetViewZoneArray(newZoneX, newZoneY);

                // Entities I follow
                // The new area between the two nine-square grids does not overlap
                foreach (var newViewZonePoint in newViewZoneArray)
                {
                    var distance = GetZoneDistance(newViewZonePoint.X, newViewZonePoint.Y, oldZoneX, oldZoneY);
                    if (distance <= 1) continue;
                    // Does not overlap with the old nine-square grid
                    ZonePointToZoneKey(newViewZonePoint.X, newViewZonePoint.Y, out var newViewZoneKey);
                    // If it already exists in the PendingZone, it is also skipped.
                    if (aoiEntity.PendingZoneKeySet.Contains(newViewZoneKey)) continue;
                    // Entities in this area are entities that have just entered line of sight
                    ScanZoneEntitysAndExclude(aoiEntity.EntityId, newViewZoneKey, enterFollowingCallback);
                }

                // First, when moving across borders, the PendingZone may need to be cleared. Clear it based on the distance.
                foreach (var pendingZoneKey in aoiEntity.PendingZoneKeySet)
                {
                    ZoneKeyToZonePoint(pendingZoneKey, out var pendingZoneX, out var pendingZoneY);
                    var distance = GetZoneDistance(newZoneX, newZoneY, pendingZoneX, pendingZoneY);
                    if (distance == 2) continue;
                    var pendingZone = _zoneDict[pendingZoneKey];
                    // After moving, the PendingZone may be drawn into the range of the nine-square grid, and the one drawn into the nine-square grid will not leave the current field of view
                    if (distance > 2)
                    {
                        // The entity in this area is the entity that just left the line of sight
                        ScanZoneEntitysAndExclude(aoiEntity.EntityId, pendingZone, leaveFollowingCallback);
                    }
                    aoiEntity.PendingZoneKeySet.Remove(pendingZoneKey);
                    pendingZone.PendingEntitySet.Remove(aoiEntity.EntityId);
                }

                // Add the original nine-square grid entity to leaveFollowingList
                foreach (var oldViewZonePoint in oldViewZoneArray)
                {
                    ZonePointToZoneKey(oldViewZonePoint.X, oldViewZonePoint.Y, out var curZoneKey);
                    var distance = GetZoneDistance(newZoneX, newZoneY, oldViewZonePoint.X, oldViewZonePoint.Y);
                    // Add the zone with a distance of 2 to Pending
                    if (distance == 2)
                    {
                        aoiEntity.PendingZoneKeySet.Add(curZoneKey);
                        var curZone = CreateZone(curZoneKey);
                        curZone.PendingEntitySet.Add(aoiEntity.EntityId);
                    }
                    // If the distance is greater than 2, add it directly to leaveFollowingList
                    else if (distance > 2)
                    {
                        ScanZoneEntitysAndExclude(aoiEntity.EntityId, curZoneKey, leaveFollowingCallback);
                    }
                }

                // The location of a person of special interest may be included in the new area of ​​interest, but no action is required.
                //foreach (var following in aoiEntity.SpecialFollowingSet)
                //{
                //    var followingAoiEntity = _entittDict[following];
                //    if (IsViewZoneEntity(aoiEntity, followingAoiEntity))
                //    {
                //        RemoveSpecialFollowing(aoiEntity, followingAoiEntity);
                //    }
                //}

                // Follow my entity
                // The new area between the two nine-square grids does not overlap
                foreach (var newViewZonePoint in newViewZoneArray)
                {
                    var distance = GetZoneDistance(newViewZonePoint.X, newViewZonePoint.Y, oldZoneX, oldZoneY);
                    if (distance <= 1) continue;
                    // A new non-overlapping area has been found, and the current entity may have been added to the line of sight of entities in that area.
                    // Check if the entity in this zone has already followed me in the old location through PendingZone
                    ZonePointToZoneKey(newViewZonePoint.X, newViewZonePoint.Y, out var newViewZoneKey);
                    ScanZoneEntitys(newViewZoneKey, e =>
                    {
                        if (e == aoiEntity.EntityId || oldZone.PendingEntitySet.Contains(e)) return;
                        enterFollowerCallback(e);
                    });
                }

                // I may leave the sight of the entity that was originally watching me through the nine-square grid
                foreach (var oldViewZonePoint in oldViewZoneArray)
                {
                    var distance = GetZoneDistance(oldViewZonePoint.X, oldViewZonePoint.Y, newZoneX, newZoneY);
                    if (distance <= 1) continue;
                    // Found non-overlapping old regions
                    // Check if the entity in this zone is following me in the new location via PendingZone
                    ZonePointToZoneKey(oldViewZonePoint.X, oldViewZonePoint.Y, out var oldViewZoneKey);
                    ScanZoneEntitys(oldViewZoneKey, e =>
                    {
                        if (e == aoiEntity.EntityId || newZone.PendingEntitySet.Contains(e)) return;
                        leaveFollowerCallback(e);
                    });
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get a list of entities I'm interested in
        /// </summary>
        /// <param name="aoiEntity"></param>
        /// <returns></returns>
        public void ScanFollowingList(AoiEntity aoiEntity, Action<int> callback)
        {
            ZoneKeyToZonePoint(aoiEntity.ZoneKey, out var x, out var y);
            var viewZoneArray = GetViewZoneArray(x, y);
            foreach (var curZonePoint in viewZoneArray)
            {
                ZonePointToZoneKey(curZonePoint.X, curZonePoint.Y, out var curZoneKey);
                ScanZoneEntitysAndExclude(aoiEntity.EntityId, curZoneKey, callback);
            }

            foreach (var pendingZoneKey in aoiEntity.PendingZoneKeySet)
            {
                ScanZoneEntitysAndExclude(aoiEntity.EntityId, pendingZoneKey, callback);
            }

            //foreach (var following in aoiEntity.SpecialFollowingSet)
            //{
            //    // If it is in the area of ​​interest, it has already been added, skip it
            //    var followingAoiEntity = _entittDict[following];
            //    if (!IsViewZoneEntity(aoiEntity, followingAoiEntity))
            //    {
            //        list.Add(following);
            //    }
            //}
        }

        /// <summary>
        /// Get a list of entities I follow and filter by radius
        /// </summary>
        /// <param name="aoiEntity"></param>
        /// <returns></returns>
        public void ScanFollowingList(AoiEntity aoiEntity, float range, Action<int> callback)
        {
            // If the diameter is less than or equal to the length of the grid, only the four nearest grids need to be obtained.
            if (range <= _zoneSize)
            {

            }
        }


        /// <summary>
        /// Get a list of entities following me
        /// </summary>
        /// <param name="aoiEntity"></param>
        /// <returns></returns>
        public void ScanFollowerList(AoiEntity aoiEntity, Action<int> callback)
        {
            ZoneKeyToZonePoint(aoiEntity.ZoneKey, out var x, out var y);
            var viewZoneArray = GetViewZoneArray(x, y);
            foreach (var curZonePoint in viewZoneArray)
            {
                ZonePointToZoneKey(curZonePoint.X, curZonePoint.Y, out var curZoneKey);
                ScanZoneEntitysAndExclude(aoiEntity.EntityId, curZoneKey, callback);
            }
            // Follow my entity in the Pending area
            var zone = _zoneDict[aoiEntity.ZoneKey];
            foreach (var entityId in zone.PendingEntitySet)
            {
                callback(entityId);
            }
        }

        /// <summary>
        /// Determine whether src is following dest
        /// </summary>
        /// <param name="aoiEntity"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsFollowing(AoiEntity src, AoiEntity dest)
        {
            return IsViewZoneEntity(src, dest);
            //|| aoiEntity.SpecialFollowingSet.Contains(dest.EntityId);
        }


        private bool IsViewZoneEntity(AoiEntity src, AoiEntity dest)
        {
            ZoneKeyToZonePoint(src.ZoneKey, out var x, out var y);
            ZoneKeyToZonePoint(dest.ZoneKey, out var targetX, out var targetY);
            if (Math.Abs(x - targetX) <= _zoneSize && Math.Abs(y - targetY) <= _zoneSize) return true;
            return src.PendingZoneKeySet.Contains(dest.ZoneKey);
        }

        private AoiZone CreateZone(UInt64 zoneKey)
        {
            if (!_zoneDict.TryGetValue(zoneKey, out var newZone))
            {
                newZone = new();
                _zoneDict[zoneKey] = newZone;
            }
            return newZone;
        }

        /// <summary>
        /// Get the visible area of ​​the nine-square grid
        /// </summary>
        private Vector2Int[] GetViewZoneArray(int x, int y)
        {
            var arr = new Vector2Int[9];
            arr[0].X = x - _zoneSize;
            arr[0].Y = y + _zoneSize;

            arr[1].X = x;
            arr[1].Y = y + _zoneSize;

            arr[2].X = x + _zoneSize;
            arr[2].Y = y + _zoneSize;

            arr[3].X = x - _zoneSize;
            arr[3].Y = y;

            arr[4].X = x;
            arr[4].Y = y;

            arr[5].X = x + _zoneSize;
            arr[5].Y = y;

            arr[6].X = x - _zoneSize;
            arr[6].Y = y - _zoneSize;

            arr[7].X = x;
            arr[7].Y = y - _zoneSize;

            arr[8].X = x + _zoneSize;
            arr[8].Y = y - _zoneSize;
            return arr;
        }

        private void PointToZonePoint(float x, float y, out int outX, out int outY)
        {
            if (x >= 0)
            {
                outX = (int)x;
                outX -= outX % _zoneSize;
            }
            else
            {
                outX = (int)(x - 1.0f);
                outX -= outX % _zoneSize + _zoneSize;
            }
            

            if (y >= 0)
            {
                outY = (int)y;
                outY -= outY % _zoneSize;
            }
            else
            {
                outY = (int)(y - 1.0f);
                outY -= outY % _zoneSize + _zoneSize;
            }
        }

        private void ZoneKeyToZonePoint(UInt64 zoneKey, out int x, out int y)
        {
            x = (int)(zoneKey >> 32);
            y = (int)zoneKey;
        }

        private void ZonePointToZoneKey(int x, int y, out UInt64 zoneKey)
        {
            zoneKey = (((UInt64)(UInt32)x) << 32) | (UInt32)y;
        }

        private int GetZoneDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2)) / _zoneSize;
        }

        private void ScanZoneEntitys(AoiZone zone, Action<int> callback)
        {
            foreach (var entityId in zone.EntitySet)
            {
                callback(entityId);
            }
        }

        private void ScanZoneEntitys(UInt64 zoneKey, Action<int> callback)
        {
            if (!_zoneDict.TryGetValue(zoneKey, out var zone)) return;
            ScanZoneEntitys(zone, callback);
        }

        private void ScanZoneEntitysAndExclude(int entityId, AoiZone zone, Action<int> callback)
        {
            ScanZoneEntitys(zone, e =>
            {
                if (entityId == e)
                {
                    return;
                }
                callback(e);
            });
        }

        private void ScanZoneEntitysAndExclude(int entityId, UInt64 zoneKey, Action<int> callback)
        {
            if (!_zoneDict.TryGetValue(zoneKey, out var zone)) return;
            ScanZoneEntitysAndExclude(entityId, zone, callback);
        }

    }
}
