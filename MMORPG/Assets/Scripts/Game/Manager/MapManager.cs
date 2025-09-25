using System;
using MMORPG.Common.Proto.Entity;
using MMORPG.Common.Proto.Player;
using MMORPG.Common.Tool;
using MMORPG.Event;
using MMORPG.Global;
using MMORPG.Model;
using QFramework;
using MMORPG.System;
using MMORPG.Tool;
using Serilog;
using ThirdPersonCamera;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace MMORPG.Game
{
    /// <summary>
    /// Map Controller
    /// Responsible for listening for character creation events in the current map and adding characters to the map
    /// </summary>
    public class MapManager : MonoBehaviour, IController, ICanSendEvent
    {
        private IPlayerManagerSystem _playerManager;
        private IEntityManagerSystem _entityManager;
        private IDataManagerSystem _dataManager;

        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }

        void Awake()
        {
            _playerManager = this.GetSystem<IPlayerManagerSystem>();
            _entityManager = this.GetSystem<IEntityManagerSystem>();
            _dataManager = this.GetSystem<IDataManagerSystem>();
        }

        public void OnJoinMap(long characterId)
        {
            var net = this.GetSystem<INetworkSystem>();
            net.SendToServer(new JoinMapRequest
            {
                CharacterId = characterId,
            });
            net.Receive<JoinMapResponse>(response =>
            {
                if (response.Error != Common.Proto.Base.NetError.Success)
                {
                    Log.Error($"JoinMap Error:{response.Error.GetInfo().Description}");
                    //TODO Error handling
                    return;
                }

                Log.Information($"JoinMap Success, MineId:{response.EntityId}");

                var unitDefine = _dataManager.GetUnitDefine(response.UnitId);

                var position = response.Transform.Position.ToVector3();
                // Perform radiographic testing to ensure entities are not stuck underground
                var rayStart = position + Vector3.up * 100f; // Shooting rays downward from a height
                var ray = new Ray(rayStart, Vector3.down);
                var layerMask = LayerMask.GetMask("Map", "Terrain");
                if (Physics.Raycast(ray, out var hit, 200f, layerMask))
                {
                    // If the ground is detected, adjust the y-axis position to be above the ground
                    position.y = hit.point.y + 0.1f; // Raise it slightly to avoid touching the ground completely
                }

                var entity = _entityManager.SpawnEntity(
                    Resources.Load<EntityView>($"{Config.PlayerPrefabsPath}/{unitDefine.Resource}"),
                    response.EntityId,
                    response.UnitId,
                    EntityType.Player,
                    position,
                    Quaternion.Euler(response.Transform.Direction.ToVector3()));

                this.GetSystem<IPlayerManagerSystem>().SetMine(entity);

                var actor = entity.GetComponent<ActorController>();
                actor.ApplyNetActor(response.Actor, true);

                Camera.main.GetComponent<CameraController>().InitFromTarget(entity.transform);
                foreach (var followTarget in FindObjectsByType<FollowTarget>(FindObjectsSortMode.None))
                {
                    followTarget.Target = entity.transform;
                }
            });
        }
    }
}
