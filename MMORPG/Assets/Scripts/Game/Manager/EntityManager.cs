using System;
using MMORPG.Common.Proto.Entity;
using MMORPG.Common.Proto.Fight;
using MMORPG.Common.Proto.Map;
using MMORPG.Event;
using MMORPG.Global;
using QFramework;
using MMORPG.System;
using MMORPG.Tool;
using Serilog;
using UnityEngine;

namespace MMORPG.Game
{
    public struct EntityTransformSyncData
    {
        public EntityView Entity;
        public Vector3 Position;
        public Quaternion Rotation;
        public int StateId;
        public byte[] Data;
    }

    public class EntityManager : MonoBehaviour, IController, ICanSendEvent
    {
        private IEntityManagerSystem _entityManager;
        private IPlayerManagerSystem _playerManager;
        private IDataManagerSystem _dataManager;
        private INetworkSystem _network;

        private void Awake()
        {
            _entityManager = this.GetSystem<IEntityManagerSystem>();
            _dataManager = this.GetSystem<IDataManagerSystem>();
            _playerManager = this.GetSystem<IPlayerManagerSystem>();
            _network = this.GetSystem<INetworkSystem>();

            _network.Receive<EntityEnterResponse>(OnEntityEnterReceived)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            _network.Receive<EntityLeaveResponse>(OnEntityLeaveReceived)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            _network.Receive<EntityTransformSyncResponse>(OnEntitySyncReceived)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            _network.Receive<EntityHurtResponse>(OnEntityHurtReceived)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            _network.Receive<EntityAttributeSyncResponse>(OnEntityAttributeSyncReceived)
                .UnRegisterWhenGameObjectDestroyed(gameObject);


        }

        private void OnEntityAttributeSyncReceived(EntityAttributeSyncResponse response)
        {
            if (_entityManager.EntityDict.TryGetValue(response.EntityId, out var entity))
            {
                var actor = entity.GetComponent<ActorController>();
                foreach (var entry in response.Entrys)
                {
                    Log.Debug($"{actor.gameObject.name}Property synchronization:{entry.Type}");
                    switch (entry.Type)
                    {
                        case EntityAttributeEntryType.Level:
                            actor.Level.Value = entry.Int32;
                            break;
                        case EntityAttributeEntryType.Gold:
                            actor.Gold = entry.Int32;
                            break;
                        case EntityAttributeEntryType.Hp:
                            actor.Hp = entry.Int32;
                            break;
                        case EntityAttributeEntryType.Mp:
                            actor.Mp = entry.Int32;
                            break;
                        case EntityAttributeEntryType.Exp:
                            actor.Exp = entry.Int32;
                            break;
                        case EntityAttributeEntryType.MaxHp:
                            actor.MaxHp = entry.Int32;
                            break;
                        case EntityAttributeEntryType.MaxExp:
                            actor.MaxExp = entry.Int32;
                            break;
                        case EntityAttributeEntryType.MaxMp:
                            actor.MaxMp = entry.Int32;
                            break;
                        case EntityAttributeEntryType.FlagState:
                            actor.FlagState.Value = (FlagStates)entry.Int32;
                            break;
                        case EntityAttributeEntryType.None:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private void OnEntityHurtReceived(EntityHurtResponse response)
        {
            if (_entityManager.EntityDict.TryGetValue(response.Info.TargetId, out var wounded))
            {
                if (_entityManager.EntityDict.TryGetValue(response.Info.AttackerInfo.AttackerId, out var attacker))
                {
                    attacker.OnHit?.Invoke(response.Info);
                    Log.Information($"'{wounded.gameObject.name}'receive'{attacker.gameObject.name}'of{response.Info.AttackerInfo.AttackerType}Attack, Deduction{response.Info.Amount}Point blood volume");
                }
                else
                {
                    Log.Information($"'{wounded.gameObject.name}'receive EntityId:{response.Info.AttackerInfo.AttackerId}(Out of sight){response.Info.AttackerInfo.AttackerType}attack({response.Info.DamageType}), deduct{response.Info.Amount}Point blood volume");
                }

                wounded.OnHurt?.Invoke(response.Info);

                this.SendEvent(new EntityHurtEvent(
                    wounded,
                    attacker,
                    response.Info));
            }
        }

        private void OnEntityLeaveReceived(EntityLeaveResponse response)
        {
            foreach (var id in response.EntityIds)
            {
                _entityManager.LeaveEntity(id);
            }
        }

        private async void OnEntityEnterReceived(EntityEnterResponse response)
        {
            // Wait for the main player to join the game first
            await _playerManager.GetMineEntityTask();
            foreach (var data in response.Datas)
            {
                var entityId = data.EntityId;
                var position = data.Transform.Position.ToVector3();
                var rotation = Quaternion.Euler(data.Transform.Direction.ToVector3());

                // Perform radiographic testing to ensure entities are not stuck underground
                var rayStart = position + Vector3.up * 100f; // Shooting rays downward from a height
                var ray = new Ray(rayStart, Vector3.down);
                var layerMask = LayerMask.GetMask("Map", "Terrain");
                if (Physics.Raycast(ray, out var hit, 200f, layerMask))
                {
                    // If the ground is detected, adjust the y-axis position to be above the ground
                    position.y = hit.point.y + 0.1f; // Raise it slightly to avoid touching the ground completely
                }
                
                var unitDefine = _dataManager.GetUnitDefine(data.UnitId);

                var path = unitDefine.Kind switch
                {
                    "Player" => Config.PlayerPrefabsPath,
                    "Monster" => Config.MonsterPrefabsPath,
                    "Npc" => Config.NpcPrefabsPath,
                    "DroppedItem" => Config.ItemsPrefabsPath,
                    _ => throw new NotImplementedException()
                };

                var entity = _entityManager.SpawnEntity(
                    Resources.Load<EntityView>($"{path}/{unitDefine.Resource}"),
                    entityId,
                    data.UnitId,
                    data.EntityType,
                    position,
                    rotation);

                if (entity.TryGetComponent<ActorController>(out var actor))
                {
                    actor.ApplyNetActor(data.Actor, true);
                }
            }
        }

        private void OnEntitySyncReceived(EntityTransformSyncResponse response)
        {
            if (_entityManager.EntityDict.TryGetValue(response.EntityId, out var entity))
            {
                var position = response.Transform.Position.ToVector3();
                position.y = entity.transform.position.y;
                var rotation = Quaternion.Euler(response.Transform.Direction.ToVector3());
                Debug.Assert(entity.EntityId == response.EntityId);
                var data = new EntityTransformSyncData
                {
                    Entity = entity,
                    Position = position,
                    Rotation = rotation,
                    StateId = response.StateId,
                    Data = response.Data.ToByteArray()
                };
                entity.OnTransformSync?.Invoke(data);
            }
        }

        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }
    }
}
