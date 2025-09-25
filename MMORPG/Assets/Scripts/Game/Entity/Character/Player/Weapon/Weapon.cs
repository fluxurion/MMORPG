using System;
using System.Collections;
using System.Linq;
using MMORPG.Common.Proto.Entity;
using MMORPG.Event;
using MMORPG.Tool;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MMORPG.Game
{
    public enum WeaponStates
    {
        Idle,
        Start,
        DelayBeforeUse,
        Use,
        DelayBetweenUses,
        Stop,
        Interrupted
    }

    public class Weapon : MonoBehaviour
    {
        public enum TriggerModes { SemiAuto, Auto }

        [FoldoutGroup("Id")]
        public int WeaponId;
        [FoldoutGroup("Use")]
        [Tooltip("How long after WeaponInputStart or TurnWeaponOn does the WeaponUse function start?")]
        public float DelayBeforeUse;
        [FoldoutGroup("Use")]
        [Tooltip("Similar cool down time")]
        public float TimeBetweenUses = 1f;
        [FoldoutGroup("Use")]
        [Tooltip("SemiAuto:Stop using weapons when you have used them up \n Auto: Continue using weapons after running out of them, Until WeaponInputStop or TurnWeaponOff is manually called")]
        public TriggerModes TriggerMode = TriggerModes.Auto;

        [FoldoutGroup("Movement")]
        [Tooltip("Prevents the character from moving when using a weapon")]
        public bool PreventAllMovementWhileInUse = false;

        [FoldoutGroup("Animator Parameter Names")]
        public string IdleAnimationParameter;
        [FoldoutGroup("Animator Parameter Names")]
        public string StartAnimationParameter;
        [FoldoutGroup("Animator Parameter Names")]
        public string DelayBeforeUseAnimationParameter;
        [FoldoutGroup("Animator Parameter Names")]
        public string UseAnimationParameter;
        [FoldoutGroup("Animator Parameter Names")]
        public string DelayBetweenUsesAnimationParameter;
        [FoldoutGroup("Animator Parameter Names")]
        public string StopAnimationParameter;

        [FoldoutGroup("Animator Trigger Parameter Names")]
        public string IdleTriggerAnimationParameter;
        [FoldoutGroup("Animator Trigger Parameter Names")]
        public string StartTriggerAnimationParameter;
        [FoldoutGroup("Animator Trigger Parameter Names")]
        public string DelayBeforeUseTriggerAnimationParameter;
        [FoldoutGroup("Animator Trigger Parameter Names")]
        public string UseTriggerAnimationParameter;
        [FoldoutGroup("Animator Trigger Parameter Names")]
        public string DelayBetweenUsesTriggerAnimationParameter;
        [FoldoutGroup("Animator Trigger Parameter Names")]
        public string StopTriggerAnimationParameter;

        [FoldoutGroup("Weapon Feedbacks")]
        public FeedbacksManager WeaponStartFeedbacks;
        [FoldoutGroup("Weapon Feedbacks")]
        public FeedbacksManager WeaponUsedFeedbacks;
        [FoldoutGroup("Weapon Feedbacks")]
        public FeedbacksManager WeaponStopFeedbacks;

        [FoldoutGroup("Settings")]
        public bool InitializeOnStart = false;
        [FoldoutGroup("Settings")]
        [Tooltip("Is it possible to use the weapon when(including cooling)interrupt")]
        public bool Interruptible = false;

        public bool CanUse => FSM.CurrentStateId == WeaponStates.Idle && !PreventFire;

        public bool PreventFire { get; set; } = false;

        private float _lastTurnWeaponOnAt = -float.MaxValue;
        private float _lastShootRequestAt = -float.MaxValue;
        private float _delayBeforeUseCounter;
        private float _delayBetweenUsesCounter;
        private bool _triggerReleased;

        public PlayerHandleWeapon HandleWeapon { get; private set; }

        public FSM<WeaponStates> FSM { get; set; }

        public event Action<Weapon> OnWeaponInitialized;
        public event Action<Weapon> OnWeaponStarted;
        public event Action<Weapon> OnWeaponStopped;

        public bool IsInitialized { get; private set; }

        protected virtual void Start()
        {
            if (InitializeOnStart)
            {
                Initialize();
            }
        }

        protected virtual void Update()
        {
            if (!IsInitialized) return;

            UpdateAnimator();
            FSM.Update();
        }


        public virtual void Setup(PlayerHandleWeapon owner)
        {
            HandleWeapon = owner;
        }

        public virtual void Initialize()
        {
            if (IsInitialized) return;

            InitFSM();

            OnWeaponInitialized?.Invoke(this);

            IsInitialized = true;
        }

        /// <summary>
        /// <para>use weapons</para>
        /// <para>1. If the current weapon is in use, it will try to interrupt it.</para>
        /// <para>2. If PreventFire is True, it will prevent the use of</para>
        /// </summary>
        public virtual void WeaponInputStart()
        {
            if (CanUse)
            {
                _triggerReleased = false;
                TurnWeaponOn();
            }
        }

        /// <summary>
        /// <para>stop weapons use</para>
        /// <para>Will stop after the current weapon is used up (including cooldown completion)</para>
        /// </summary>
        public virtual void WeaponInputStop()
        {
            _triggerReleased = true;
        }

        /// <summary>
        /// <para>Use weapons directly</para>
        /// <para>Tip: WeaponInputStart is recommended.</para>
        /// </summary>
        public virtual void TurnWeaponOn()
        {
            if (Time.time - _lastTurnWeaponOnAt < TimeBetweenUses || PreventFire)
            {
                return;
            }
            _lastTurnWeaponOnAt = Time.time;

            FSM.ChangeState(WeaponStates.Start);
            OnWeaponStarted?.Invoke(this);
        }

        /// <summary>
        /// <para>Forced cessation of weapon use</para>
        /// <para>Tip: WeaponInputStop is recommended</para>
        /// </summary>
        public virtual void TurnWeaponOff()
        {
            if (FSM.CurrentStateId is WeaponStates.Idle or WeaponStates.Stop)
            {
                return;
            }
            _triggerReleased = true;
            FSM.ChangeState(WeaponStates.Stop);
            OnWeaponStopped?.Invoke(this);
        }

        protected virtual void InitFSM()
        {
            FSM ??= new();

            FSM.State(WeaponStates.Idle).OnUpdate(CaseWeaponIdle);
            FSM.State(WeaponStates.Start).OnUpdate(CaseWeaponStart);
            FSM.State(WeaponStates.DelayBeforeUse).OnUpdate(CaseWeaponDelayBeforeUse);
            FSM.State(WeaponStates.Use).OnUpdate(CaseWeaponUse);
            FSM.State(WeaponStates.DelayBetweenUses).OnUpdate(CaseWeaponDelayBetweenUses);
            FSM.State(WeaponStates.Stop).OnUpdate(CaseWeaponStop);
            FSM.State(WeaponStates.Interrupted).OnUpdate(CaseWeaponInterrupted);

            FSM.StartState(WeaponStates.Idle);

            FSM.OnStateChanged((prev, cur) =>
            {
                switch (cur)
                {
                    case WeaponStates.Idle:
                        if (!string.IsNullOrEmpty(IdleTriggerAnimationParameter))
                            HandleWeapon.Brain.AnimationController.Animator.SetTrigger(IdleTriggerAnimationParameter);
                        break;
                    case WeaponStates.Start:
                        if (!string.IsNullOrEmpty(StartTriggerAnimationParameter))
                            HandleWeapon.Brain.AnimationController.Animator.SetTrigger(StartTriggerAnimationParameter);
                        break;
                    case WeaponStates.DelayBeforeUse:
                        if (!string.IsNullOrEmpty(DelayBeforeUseTriggerAnimationParameter))
                            HandleWeapon.Brain.AnimationController.Animator.SetTrigger(DelayBeforeUseTriggerAnimationParameter);
                        break;
                    case WeaponStates.Use:
                        if (!string.IsNullOrEmpty(UseTriggerAnimationParameter))
                            HandleWeapon.Brain.AnimationController.Animator.SetTrigger(UseTriggerAnimationParameter);
                        break;
                    case WeaponStates.DelayBetweenUses:
                        if (!string.IsNullOrEmpty(DelayBetweenUsesTriggerAnimationParameter))
                            HandleWeapon.Brain.AnimationController.Animator.SetTrigger(DelayBetweenUsesTriggerAnimationParameter);
                        break;
                    case WeaponStates.Stop:
                        if (!string.IsNullOrEmpty(StopTriggerAnimationParameter))
                            HandleWeapon.Brain.AnimationController.Animator.SetTrigger(StopTriggerAnimationParameter);
                        break;
                    case WeaponStates.Interrupted:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(cur), cur, null);
                }
            });
        }

        protected virtual void CaseWeaponIdle()
        {
        }

        protected virtual void CaseWeaponStart()
        {
            if (WeaponStartFeedbacks != null)
                WeaponStartFeedbacks.Play();
            if (DelayBeforeUse > 0)
            {
                _delayBeforeUseCounter = DelayBeforeUse;
                FSM.ChangeState(WeaponStates.DelayBeforeUse);
            }
            else
            {
                StartCoroutine(ShootRequestCo());
            }

            if (PreventAllMovementWhileInUse)
                HandleWeapon.Brain.ActorController.PreventMovement();
        }

        protected virtual void CaseWeaponDelayBeforeUse()
        {
            _delayBeforeUseCounter -= Time.deltaTime;
            if (_delayBeforeUseCounter <= 0)
            {
                StartCoroutine(ShootRequestCo());
            }
        }

        protected virtual IEnumerator ShootRequestCo()
        {
            if (Time.time - _lastShootRequestAt < TimeBetweenUses)
            {
                yield break;
            }
            ShootRequest();
            _lastShootRequestAt = Time.time;
        }

        protected virtual void CaseWeaponUse()
        {
            WeaponUse();
            _delayBetweenUsesCounter = TimeBetweenUses;
            FSM.ChangeState(WeaponStates.DelayBetweenUses);
        }

        protected virtual void CaseWeaponDelayBetweenUses()
        {
            if (_triggerReleased)
            {
                TurnWeaponOff();
                return;
            }

            _delayBetweenUsesCounter -= Time.deltaTime;
            if (_delayBetweenUsesCounter <= 0)
            {
                if (TriggerMode == TriggerModes.Auto && !_triggerReleased)
                {
                    StartCoroutine(ShootRequestCo());
                }
                else
                {
                    TurnWeaponOff();
                }
            }
        }

        protected virtual void CaseWeaponInterrupted()
        {
            TurnWeaponOff();
            FSM.ChangeState(WeaponStates.Idle);
        }

        protected virtual void CaseWeaponStop()
        {
            if (WeaponStopFeedbacks != null)
                WeaponStopFeedbacks.Play();
            FSM.ChangeState(WeaponStates.Idle);
            if (PreventAllMovementWhileInUse)
                HandleWeapon.Brain.ActorController.StopPreventMovement();
        }

        protected virtual void ShootRequest()
        {
            FSM.ChangeState(WeaponStates.Use);
        }

        protected virtual void WeaponUse()
        {
            if (WeaponUsedFeedbacks != null)
                WeaponUsedFeedbacks.Play();
        }

        protected virtual void OnWeaponUse()
        {
        }

        protected virtual void UpdateAnimator()
        {
            if (!IdleAnimationParameter.IsNullOrEmpty())
                HandleWeapon.Brain.ActorController.Animator.SetBool(IdleAnimationParameter, FSM.CurrentStateId == WeaponStates.Idle);

            if (!StartAnimationParameter.IsNullOrEmpty())
                HandleWeapon.Brain.ActorController.Animator.SetBool(StartAnimationParameter, FSM.CurrentStateId == WeaponStates.Start);

            if (!DelayBeforeUseAnimationParameter.IsNullOrEmpty())
                HandleWeapon.Brain.ActorController.Animator.SetBool(DelayBeforeUseAnimationParameter, FSM.CurrentStateId == WeaponStates.DelayBeforeUse);

            if (!UseAnimationParameter.IsNullOrEmpty())
                HandleWeapon.Brain.ActorController.Animator.SetBool(UseAnimationParameter, FSM.CurrentStateId == WeaponStates.Use);

            if (!DelayBetweenUsesAnimationParameter.IsNullOrEmpty())
                HandleWeapon.Brain.ActorController.Animator.SetBool(DelayBetweenUsesAnimationParameter, FSM.CurrentStateId == WeaponStates.DelayBetweenUses);

            if (!StopAnimationParameter.IsNullOrEmpty())
                HandleWeapon.Brain.ActorController.Animator.SetBool(StopAnimationParameter, FSM.CurrentStateId == WeaponStates.Stop);
        }

        protected virtual void OnDestroy()
        {
            FSM?.Clear();
        }
    }
}
