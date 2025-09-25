using System.Diagnostics;
using Aoi;
using MMORPG.Common.Proto.Entity;
using MMORPG.Common.Proto.Map;
using GameServer.Tool;
using Serilog;
using Google.Protobuf;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Principal;
using GameServer.NpcSystem;
using GameServer.EntitySystem;
using GameServer.FightSystem;
using GameServer.InventorySystem;
using GameServer.PlayerSystem;
using GameServer.MissileSystem;
using GameServer.MonsterSystem;

namespace GameServer.MapSystem
{
    public class Map
    {
        const int InvalidMapId = 0;

        public MapDefine Define { get; }
        public int MapId => Define.ID;

        public PlayerManager PlayerManager { get; }
        public MonsterManager MonsterManager { get; }
        public NpcManager NpcManager { get; }
        public MissileManager MissileManager { get; }
        public SpawnManager SpawnManager { get; }
        public DroppedItemManager DroppedItemManager { get; }

        private AoiWord _aoiWord;

        public override string ToString()
        {
            return $"Map:\"{Define.Name}:{MapId}\"";
        }

        public Map(MapDefine mapDefine)
        {
            Define = mapDefine;

            _aoiWord = new(20);

            PlayerManager = new(this);
            MonsterManager = new(this);
            NpcManager = new(this);
            MissileManager = new(this);
            SpawnManager = new(this);
            DroppedItemManager = new(this);
        }

        public void Start()
        {
            PlayerManager.Start();
            MonsterManager.Start();
            NpcManager.Start();
            MissileManager.Start();
            SpawnManager.Start();
            DroppedItemManager.Start();
        }

        public void Update()
        {
            PlayerManager.Update();
            MonsterManager.Update();
            NpcManager.Update();
            MissileManager.Update();
            DroppedItemManager.Update();
            SpawnManager.Update();
        }

        /// <summary>
        /// Broadcast entity enters the scene
        /// </summary>
        public void EntityEnter(Entity entity)
        {
            Log.Information($"{entity}Enter{entity.Map}");

            entity.AoiEntity = _aoiWord.Enter(entity.EntityId, entity.Position.X, entity.Position.Y);
            
            var res = new EntityEnterResponse();
            res.Datas.Add(ConstructEntityEnterData(entity));

            // Broadcasts the addition of a new entity to the scene to all characters that can observe it.
            PlayerManager.Broadcast(res, entity);

            // If the new entity is a player
            // Deliver entities already in the scene to the new player that are within their visual range.
            if (entity.EntityType == EntityType.Player)
            {
                res.Datas.Clear();
                ScanEntityFollowing(entity, e =>
                {
                    res.Datas.Add(ConstructEntityEnterData(e));
                });
                var currentPlayer = entity as Player;
                currentPlayer?.User.Channel.Send(res);
            }
        }

        private EntityEnterData ConstructEntityEnterData(Entity entity)
        {
            var data = new EntityEnterData()
            {
                EntityId = entity.EntityId,
                UnitId = entity.UnitDefine.ID,
                EntityType = entity.EntityType,
                Transform = ProtoHelper.ToNetTransform(entity.Position, entity.Direction),
            };
            if (entity is Actor actor)
            {
                data.Actor = actor.ToNetActor();
            }
            return data;
        }

        /// <summary>
        ///  The broadcasting entity leaves the scene
        /// </summary>
        public void EntityLeave(Entity entity)
        {
            Log.Information($"{entity}leave{entity.Map}");

            // Broadcasts that the entity has left the scene to all characters that can observe it.
            // In fact, direct broadcast is broadcast to the entity that follows the current entity rather than the entity that follows the current entity.
            // If the field of view of all entities is consistent, there will be no problem, but if it is inconsistent, you need to consider additional maintenance
            var res = new EntityLeaveResponse();
            res.EntityIds.Add(entity.EntityId);
            PlayerManager.Broadcast(res, entity);
            _aoiWord.Leave(entity.AoiEntity);
        }

        /// <summary>
        /// Synchronizes the entity's position and broadcasts a message to players who can observe the entity.
        /// </summary>
        /// <param name="entity"></param>
        public void EntityRefreshPosition(Entity entity)
        {
            var enterRes = new EntityEnterResponse();
            enterRes.Datas.Add(ConstructEntityEnterData(entity));

            var leaveRes = new EntityLeaveResponse();
            leaveRes.EntityIds.Add(entity.EntityId);

            bool init1 = false, init2 = false;

            _aoiWord.Refresh(entity.AoiEntity, entity.Position.X, entity.Position.Y,
                entityId =>
                {
                    if (init1 == false)
                    {
                        enterRes.Datas.Clear();
                        init1 = true;
                    }
                    // If the player is moving, the player also needs to be notified of all new entities that have entered the field of view.
                    // Log.Debug($"[Map.EntityRefreshPosition]2.entity：{entityId} 进入了 entity：{entity.EntityId} Line of sight range");
                    if (entity.EntityType != EntityType.Player) return;
                    var enterEntity = EntityManager.Instance.GetEntity(entityId);
                    Debug.Assert(enterEntity != null);
                    enterRes.Datas.Add(ConstructEntityEnterData(enterEntity));
                },
                entityId =>
                {
                    if (init2 == false)
                    {
                        leaveRes.EntityIds.Clear();
                        init2 = true;
                    }
                    // If the player is moving, the player also needs to be notified of all entities that have left their field of view.
                    // Log.Debug($"[Map.EntityRefreshPosition]2.实体：{entityId} left the entity：{entity.EntityId} Line of sight range");
                    if (entity.EntityType != EntityType.Player) return;
                    var leaveEntity = EntityManager.Instance.GetEntity(entityId);
                    Debug.Assert(leaveEntity != null);
                    leaveRes.EntityIds.Add(leaveEntity.EntityId);
                },
                entityId =>
                {
                    // If a player enters line of sight, notify them that an entity has joined
                    // Log.Debug($"[Map.EntityRefreshPosition]1.entity：{entity.EntityId} Entered the entity：{entityId} Line of sight range");
                    var enterEntity = EntityManager.Instance.GetEntity(entityId);
                    Debug.Assert(enterEntity != null);
                    if (enterEntity.EntityType != EntityType.Player) return;

                    var player = enterEntity as Player;
                    Debug.Assert(player != null);
                    player?.User.Channel.Send(enterRes);
                },
                entityId =>
                {
                    // If a player leaves line of sight, notify them that an entity has exited.
                    // Log.Debug($"[Map.EntityRefreshPosition]1.entity：{entity.EntityId} left the entity：{entityId} Line of sight range");
                    var leaveEntity = EntityManager.Instance.GetEntity(entityId);
                    Debug.Assert(leaveEntity != null);
                    if (leaveEntity.EntityType != EntityType.Player) return;

                    var player = leaveEntity as Player;
                    Debug.Assert(player != null);
                    player?.User.Channel.Send(leaveRes);
                });
        
            if (entity.EntityType == EntityType.Player)
            {
                var player = entity as Player;
                Debug.Assert(player != null);
                if (init1)
                {
                    player?.User.Channel.Send(enterRes);
                }
                if (init2)
                {
                    player?.User.Channel.Send(leaveRes);
                }
            }
        }

        /// <summary>
        /// Get entities within the specified entity's sight range
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public void ScanEntityFollowing(Entity entity, Action<Entity> callback)
        {
            _aoiWord.ScanFollowingList(entity.AoiEntity, followingEntityId =>
            {
                var followingEntity = EntityManager.Instance.GetEntity(followingEntityId);
                if (followingEntity != null) callback(followingEntity);
            });
        }

        /// <summary>
        /// Get entities within the specified entity's sight range by radius
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public void ScanEntityFollowing(Entity entity, float range, Action<Entity> callback)
        {
        }


        /// <summary>
        /// Get entities within the specified entity's sight range
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public void ScanEntityFollower(Entity entity, Action<Entity> callback)
        {
            _aoiWord.ScanFollowerList(entity.AoiEntity, followerEntityId =>
            {
                var followerEntity = EntityManager.Instance.GetEntity(followerEntityId);
                if (followerEntity != null) callback(followerEntity);
            });
        }

        public Entity? GetEntityFollowingNearest(Entity entity, Predicate<Entity>? condition = null)
        {
            Entity? nearest = null;
            float minDistance = 0;
            _aoiWord.ScanFollowerList(entity.AoiEntity, followerEntityId =>
            {
                var followerEntity = EntityManager.Instance.GetEntity(followerEntityId);
                if (followerEntity != null && (condition == null || condition(followerEntity)))
                {
                    if (nearest == null)
                    {
                        nearest = followerEntity;
                        minDistance = Vector2.Distance(followerEntity.Position, entity.Position);
                    }
                    else
                    {
                        var tmp = Vector2.Distance(followerEntity.Position, entity.Position);
                        if (tmp < minDistance)
                        {
                            nearest = followerEntity;
                            minDistance = tmp;
                        }
                    }
                }
            });
            return nearest;
        }

        /// <summary>
        /// Update the entity based on the network entity object and broadcast the new state
        /// </summary>
        public void EntityTransformSync(int entityId, NetTransform transform, int stateId, ByteString data)
        {
            var entity = EntityManager.Instance.GetEntity(entityId);
            if (entity == null) return;

            entity.Position = transform.Position.ToVector3().ToVector2();
            entity.Direction = transform.Direction.ToVector3();
            EntityRefreshPosition(entity);

            var response = new EntityTransformSyncResponse
            {
                EntityId = entityId,
                Transform = transform,
                StateId = stateId,
                Data = data
            };

            // Broadcasts the new entity's status update to all actors
            PlayerManager.Broadcast(response, entity);
        }

        /// <summary>
        /// Update the entity based on the server entity object and broadcast the new state
        /// </summary>
        public void EntitySync(int entityId, int stateId)
        {
            var entity = EntityManager.Instance.GetEntity(entityId);
            if (entity == null) return;
            EntityRefreshPosition(entity);

            var response = new EntityTransformSyncResponse
            {
                EntityId = entityId,
                Transform = new()
                {
                    Direction = entity.Position.ToVector3().ToNetVector3(),
                    Position = entity.Direction.ToNetVector3()
                },
                StateId = stateId,
                Data = null
            };

            // Broadcasts the new entity's status update to all actors
            PlayerManager.Broadcast(response, entity);
        }

    }
}
