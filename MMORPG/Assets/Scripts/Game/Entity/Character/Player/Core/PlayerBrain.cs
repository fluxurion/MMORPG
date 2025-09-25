using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MMORPG.Common.Proto.Entity;
using Google.Protobuf;
using MMORPG.Command;
using MMORPG.Common.Proto.Fight;
using MMORPG.Event;
using MMORPG.Global;
using MMORPG.System;
using MMORPG.Tool;
using MMORPG.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using QFramework;

namespace MMORPG.Game
{
    public class PlayerBrain : MonoBehaviour, IController
    {
        [Required]
        public ActorController ActorController;
        [Required]
        public PlayerAnimationController AnimationController;

        public float ForcePower = 10f;
        public float ReviveTime = 5f;

#if UNITY_EDITOR
        [SerializeField]
        [ReadOnly]
        [LabelText("CurrentState")]
        private string _currentStateName = "NONE";
#endif

        // public string StartStateName = string.Empty;

        [Information("There are errors in the state machine that have not been processed!", InfoMessageType.Error, "CheckStatesHasError")]
        [Information("An empty state machine is meaningless!", InfoMessageType.Warning, "IsEmptyStates")]
        [Information("There cannot be a state with the same name!", InfoMessageType.Error, "HasRepeatStateName")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Name")]
        public PlayerState[] States;

        public PlayerState CurrentState { get; private set; }

        [Title("Binding")]
        public PlayerHandleWeapon HandleWeapon;
        public GameObject[] AdditionalAbilityNodes;

        public bool IsDeath => CurrentState.Name == "Death";

        public GameInputControls InputControls { get; private set; }

        public LocalPlayerAbility[] GetAttachLocalAbilities() => GetAttachAbilities<LocalPlayerAbility>();

        public RemotePlayerAbility[] GetAttachRemoteAbilities() => GetAttachAbilities<RemotePlayerAbility>();

        public Vector2 GetMoveInput() => InputManager.CanInput ? InputControls.Player.Move.ReadValue<Vector2>() : Vector2.zero;
        public bool IsPressingRun() => InputManager.CanInput && InputControls.Player.Run.inProgress;

        private bool? _isMine = null;

        public bool IsMine
        {
            get
            {
                if (_isMine == null)
                {
                    var mine = this.GetSystem<IPlayerManagerSystem>().MineEntity;
                    if (mine == null)
                        throw new Exception("Player: Not initialized yet!");
                    _isMine = mine.EntityId == ActorController.Entity.EntityId;
                }

                return _isMine == true;
            }
        }

        private INetworkSystem _newtwork;
        private IEntityManagerSystem _entityManager;

        private TAbility[] GetAttachAbilities<TAbility>() where TAbility : PlayerAbility
        {
            var total = new List<TAbility>();
            total.AddRange(GetComponents<TAbility>());
            foreach (var node in AdditionalAbilityNodes)
            {
                total.AddRange(node.GetComponents<TAbility>());
            }
            return total.ToArray();
        }

        public void ChangeStateByName(string stateName)
        {
            ChangeState(GetState(stateName));
        }

        public void ChangeState(PlayerState state)
        {
            Debug.Assert(state != null);
            Debug.Assert(States.Contains(state));
            CurrentState?.Exit();
            CurrentState = state;
#if UNITY_EDITOR
            _currentStateName = CurrentState.Name;
#endif
            CurrentState.Enter();
        }

        public PlayerState GetState(string stateName)
        {
            return Array.Find(States, x => x.Name == stateName);
        }

        public void NetworkUploadTransform(int stateId, byte[] data = null)
        {
            _newtwork.SendToServer(new EntityTransformSyncRequest()
            {
                EntityId = ActorController.Entity.EntityId,
                Transform = new()
                {
                    Direction = ActorController.Entity.transform.rotation.eulerAngles.ToNetVector3(),
                    Position = ActorController.Entity.transform.position.ToNetVector3()
                },
                StateId = stateId,
                Data = data == null ? ByteString.Empty : ByteString.CopyFrom(data)
            });
        }

        private void Awake()
        {
#if UNITY_EDITOR
            if (CheckStatesHasError())
            {
                UnityEditor.EditorUtility.DisplayDialog("mistake", "Player of PlayerBrain The state machine in has an error that has not been handled!", "Sure");
                UnityEditor.EditorApplication.isPlaying = false;
                return;
            }
#endif
            _newtwork = this.GetSystem<INetworkSystem>();
            _entityManager = this.GetSystem<IEntityManagerSystem>();
            AnimationController.Setup(this);
            CurrentState = null;
            if (States.Length == 0) return;
            ActorController.Entity.OnTransformSync += OnTransformEntitySync;
            ActorController.Entity.OnHurt += info =>
            {
                if (IsDeath || info.IsMiss) return;
                if (ActorController.Hp - info.Amount <= 0)
                {
                    ChangeStateByName("Death");
                }
                else
                {
                    if (ActorController.SkillManager.CurrentSpellingSkill == null)
                    {
                        ChangeStateByName("Hurt");
                    }
                }
            };

            if (HandleWeapon != null)
                HandleWeapon.Setup(this);
        }

        private void OnTransformEntitySync(EntityTransformSyncData data)
        {
            Debug.Assert(!IsMine);
            var state = States[data.StateId];
            Debug.Assert(state != null);
            if (state != CurrentState)
            {
                ChangeState(state);
            }

            foreach (var action in state.Actions)
            {
                action.TransformEntitySync(data);
            }
        }

        private void Start()
        {
            if (IsMine)
            {
                InputControls = new();
                InputControls.Enable();

                InputControls.Player.Pickup.started += context =>
                {
                    //TODO pick up
                    this.SendCommand(new PickupItemCommand());
                };
            }
            else
            {
                // Destroy(ActorController.Rigidbody);
                // Destroy(ActorController.Collider);
            }

            if (States.IsNullOrEmpty()) return;
            InitStates();
            ChangeState(States[0]);
            StartCoroutine(NetworkFixedUpdate());

            var dataManagerSystem = this.GetSystem<IDataManagerSystem>();
            this.RegisterEvent<PickupItemEvent>(e =>
            {
                var itemDefine = dataManagerSystem.GetItemDefine(e.Resp.ItemId);

                if (itemDefine == null)
                {
                    return;
                }

                UITipPanel.Content = $"you picked it up[{itemDefine.Name}] × {e.Resp.Amount}";

                //TODO Play sound effect after picking up successfully
                SoundManager.Instance.PlayerPickItemAudio.Play();
            });

            if (ActorController.Hp == 0)
            {
                ChangeStateByName("Death");
            }
        }

        private void Update()
        {
            if (States.IsNullOrEmpty()) return;
            CurrentState?.Update();
        }

        private void FixedUpdate()
        {
            if (States.IsNullOrEmpty()) return;
            CurrentState?.FixedUpdate();
        }


        private IEnumerator NetworkFixedUpdate()
        {
            while (true)
            {
                CurrentState?.NetworkFixedUpdate();
                yield return new WaitForSeconds(Config.NetworkUpdateDeltaTime);
            }
        }

        private void OnEnable()
        {
            InputControls?.Enable();
        }

        private void OnDisable()
        {
            InputControls?.Disable();
        }

        private void InitStates()
        {
            GetAttachLocalAbilities().ForEach(ability => ability.gameObject.SetActive(true));
            GetAttachRemoteAbilities().ForEach(ability => ability.gameObject.SetActive(true));

            for (int i = 0; i < States.Length; i++)
            {
                var state = States[i];
                
                state.Setup(this, i);
                state.Initialize();
                state.OnTransitionEvaluated += (transition, condition) =>
                {
                    ChangeState(condition ? transition.TrueState : transition.FalseState);
                };
            }
        }


#if UNITY_EDITOR
        [OnInspectorGUI]
        private void OnInspectorGUI()
        {
            foreach (var state in States)
            {
                state.Brain = this;
            }
        }

        private bool HasRepeatStateName => States.GroupBy(x => x.Name).Any(g => g.Count() > 1);

        private bool IsEmptyStates => States.Length == 0;

        private bool CheckStatesHasError()
        {
            return States.Any(x => x.HasError());
        }
#endif
        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }
    }

}
