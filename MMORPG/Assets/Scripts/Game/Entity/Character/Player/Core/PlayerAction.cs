using System;
using System.Collections;
using System.Linq;
using MMORPG.Tool;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MMORPG.Game
{
    [Serializable]
    public class PlayerAction
    {
        [Information("Invalid Ability!", InfoMessageType.Error, "CheckLocalAbilityNameInvalid")]
        [VerticalGroup("Local Ability")]
        [ValueDropdown("GetLocalAbilityDropdown")]
        [HideLabel]
        public string LocalAbilityName = string.Empty;

        [Information("Invalid Ability!", InfoMessageType.Error, "CheckRemoteAbilityNameInvalid")]
        [VerticalGroup("Remote Ability")]
        [ValueDropdown("GetRemoteAbilityDropdown")]
        [HideLabel]
        public string RemoteAbilityName = string.Empty;

        public LocalPlayerAbility LocalAbility { get; private set; }
        public RemotePlayerAbility RemoteAbility { get; private set; }
        public PlayerState OwnerState { get; set; }
        public int OwnerStateId { get; set; }

        public void Setup(PlayerState state, int stateId)
        {
            OwnerState = state;
            OwnerStateId = stateId;
        }

        public void Initialize()
        {
            if (OwnerState.Brain.IsMine)
            {
                LocalAbility = OwnerState.Brain.GetAttachLocalAbilities()
                    .First(x => x.GetType().Name == LocalAbilityName);

                LocalAbility.OwnerState = OwnerState;
                LocalAbility.Brain = OwnerState.Brain;
                LocalAbility.OwnerStateId = OwnerStateId;

                LocalAbility.OnStateInit();
            }
            else
            {
                RemoteAbility = OwnerState.Brain.GetAttachRemoteAbilities()
                    .First(x => x.GetType().Name == RemoteAbilityName);

                RemoteAbility.OwnerState = OwnerState;
                RemoteAbility.Brain = OwnerState.Brain;
                RemoteAbility.OwnerStateId = OwnerStateId;

                RemoteAbility.OnStateInit();
            }
        }

        public void Enter()
        {
            if (OwnerState.Brain.IsMine)
            {
                LocalAbility.OwnerState = OwnerState;
                LocalAbility.Brain = OwnerState.Brain;
                LocalAbility.OwnerStateId = OwnerStateId;
                if (LocalAbility.EnterAbilityFeedbacks != null)
                    LocalAbility.EnterAbilityFeedbacks.Play();
                LocalAbility.OnStateEnter();
            }
            else
            {
                RemoteAbility.OwnerState = OwnerState;
                RemoteAbility.Brain = OwnerState.Brain;
                RemoteAbility.OwnerStateId = OwnerStateId;
                if (RemoteAbility.EnterAbilityFeedbacks != null)
                    RemoteAbility.EnterAbilityFeedbacks.Play();
                RemoteAbility.OnStateEnter();
            }
        }

        public void Update()
        {
            AssertCheck();
            if (OwnerState.Brain.IsMine)
                LocalAbility.OnStateUpdate();
            else
                RemoteAbility.OnStateUpdate();
        }

        public void FixedUpdate()
        {
            AssertCheck();
            if (OwnerState.Brain.IsMine)
                LocalAbility.OnStateFixedUpdate();
            else
                RemoteAbility.OnStateFixedUpdate();
        }

        public void NetworkFixedUpdate()
        {
            AssertCheck();
            if (OwnerState.Brain.IsMine)
                LocalAbility.OnStateNetworkFixedUpdate();
            else
                RemoteAbility.OnStateNetworkFixedUpdate();
        }

        public void Exit()
        {
            AssertCheck();
            if (OwnerState.Brain.IsMine)
            {
                if (LocalAbility.ExitAbilityFeedbacks != null)
                    LocalAbility.ExitAbilityFeedbacks.Play();
                LocalAbility.OnStateExit();
            }
            else
            {
                if (RemoteAbility.ExitAbilityFeedbacks != null)
                    RemoteAbility.ExitAbilityFeedbacks.Play();
                RemoteAbility.OnStateExit();
            }
        }

        public void TransformEntitySync(EntityTransformSyncData data)
        {
            Debug.Assert(!OwnerState.Brain.IsMine);
            AssertCheck();
            RemoteAbility.OnStateNetworkSyncTransform(data);
        }

        public void AssertCheck()
        {
            if (OwnerState.Brain.IsMine)
            {
                Debug.Assert(LocalAbility.OwnerState == OwnerState);
                Debug.Assert(LocalAbility.Brain == OwnerState.Brain);
                Debug.Assert(LocalAbility.OwnerStateId == OwnerStateId);
            }
            else
            {
                Debug.Assert(RemoteAbility.OwnerState == OwnerState);
                Debug.Assert(RemoteAbility.Brain == OwnerState.Brain);
                Debug.Assert(RemoteAbility.OwnerStateId == OwnerStateId);
            }
        }

#if UNITY_EDITOR
        private IEnumerable GetLocalAbilityDropdown()
        {
            var total = new ValueDropdownList<string> { { "None Local Ability", string.Empty } };
            if (OwnerState == null || OwnerState.Brain == null) return total;

            var abilities = OwnerState.Brain.GetAttachLocalAbilities();
            if (abilities == null)
                return total;
            total.AddRange(abilities.Select((x, i) =>
                new ValueDropdownItem<string>($"{i} - {x.GetType().Name}", x.GetType().Name))
            );

            return total;
        }

        private IEnumerable GetRemoteAbilityDropdown()
        {
            var total = new ValueDropdownList<string> { { "None Remote Ability", string.Empty } };
            if (OwnerState == null || OwnerState.Brain == null) return total;

            var abilities = OwnerState.Brain.GetAttachRemoteAbilities();
            if (abilities == null)
                return total;
            total.AddRange(abilities.Select((x, i) =>
                new ValueDropdownItem<string>($"{i} - {x.GetType().Name}", x.GetType().Name))
            );

            return total;
        }


        private bool CheckLocalAbilityNameInvalid()
        {
            if (OwnerState?.Brain == null)
                return false;
            if (LocalAbilityName.IsNullOrEmpty())
                return true;
            var ability = OwnerState.Brain.GetAttachLocalAbilities()
                ?.FirstOrDefault(x => x.GetType().Name == LocalAbilityName);
            if (ability == null)
                return true;
            return false;
        }


        private bool CheckRemoteAbilityNameInvalid()
        {
            if (OwnerState?.Brain == null)
                return false;
            if (RemoteAbilityName.IsNullOrEmpty())
                return true;
            var ability = OwnerState.Brain.GetAttachRemoteAbilities()
                ?.FirstOrDefault(x => x.GetType().Name == RemoteAbilityName);
            if (ability == null)
                return true;
            return false;
        }

        public bool HasError()
        {
            return CheckLocalAbilityNameInvalid() || CheckRemoteAbilityNameInvalid();
        }
#endif
    }

}
