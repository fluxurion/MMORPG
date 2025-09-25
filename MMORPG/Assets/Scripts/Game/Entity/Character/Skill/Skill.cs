using System;
using System.Collections;
using MMORPG.Common.Proto.Entity;
using QFramework;
using Serilog;
using UnityEngine;

namespace MMORPG.Game
{
    public enum SkillTargetTypes
    {
        Unit,
        Position,
        None
    }

    public enum SkillModes
    {
        Combo,
        Skill
    }

    public class Skill
    {
        public enum States
        {
            Idle,
            Running,
            Cooling
        }

        public CharacterSkillManager SkillManager { get; }
        public SkillDefine Define { get; }

        public SkillTargetTypes TargetType { get; }
        public SkillModes Mode { get; }

        public States CurrentState { get; private set; }

        public float RemainCd { get; private set; }

        public event Action OnStateChanged; 

        private PlayerHandleWeapon _handleWeapon;
        private Coroutine _coroutine;

        public Skill(CharacterSkillManager skillManager, SkillDefine define)
        {
            SkillManager = skillManager;
            Define = define;

            _handleWeapon = skillManager.ActorController.GetComponentInChildren<PlayerHandleWeapon>();

            TargetType = define.TargetType switch
            {
                "Unit" => SkillTargetTypes.Unit,
                "Position" => SkillTargetTypes.Position,
                _ => SkillTargetTypes.None
            };

            Mode = define.Mode switch
            {
                "Combo" => SkillModes.Combo,
                "Skill" => SkillModes.Skill,
                _ => throw new Exception("Unknown skill mode")
            };
        }

        public void Update()
        {
            if (CurrentState == States.Cooling)
            {
                if (RemainCd <= 0f)
                {
                    ChangeState(States.Idle);
                }
                else
                {
                    RemainCd -= Time.deltaTime;
                }
            }
        }

        public void Use(CastTarget target)
        {
            Log.Debug($"{SkillManager.ActorController.Entity.EntityId}Use skills{Define.Name}");
            switch (Mode)
            {
                case SkillModes.Combo:
                    UseCombo(target);
                    break;
                case SkillModes.Skill:
                    UseSkill(target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UseCombo(CastTarget target)
        {
            if (_handleWeapon == null)
                throw new Exception($"Combo Mode Skills({Define.Name})Must be released by the object holding the PlayerHandleWeapon!");
            _handleWeapon.CurrentComboWeapon.ChangeCombo(Define.ID);
            Debug.Assert(_handleWeapon.CurrentWeapon.WeaponId == Define.ID);
            _handleWeapon.CurrentWeapon.TurnWeaponOn();
        }

        private void UseSkill(CastTarget target)
        {
            if (SkillManager.ActorController.Entity.EntityType == EntityType.Player)
            {
                if (SkillManager.CurrentSpellingSkill != null)
                {
                    Log.Warning($"{SkillManager.ActorController.Entity.EntityId}Try to release skills({SkillManager.CurrentSpellingSkill.Define.Name})Use other skills when:{Define.Name}");
                    return;
                }
                if (CurrentState != States.Idle)
                {
                    Log.Warning($"{SkillManager.ActorController.Entity.EntityId}Try using a skill that is on cooldown:{Define.Name}");
                    return;
                }
            }
            else
            {
                if (_coroutine != null)
                {
                    SkillManager.ActorController.StopCoroutine(_coroutine);
                    _coroutine = null;
                }
            }
            _coroutine = SkillManager.ActorController.StartCoroutine(SpellSkillCo(target));
        }

        public IEnumerator SpellSkillCo(CastTarget target)
        {
            SkillManager.ActorController.Animator.SetTrigger(Define.Anim2);

            if (SkillManager.ActorController.EffectManager != null)
                SkillManager.ActorController.EffectManager.TriggerEffect(Define.ID);

            SkillManager.CurrentSpellingSkill = this;
            ChangeState(States.Running);
            yield return new WaitForSeconds(Define.Duration);
            SkillManager.CurrentSpellingSkill = null;
            ChangeState(States.Cooling);

            RemainCd = Define.Cd;
        }

        public void ChangeState(States state)
        {
            CurrentState = state;
            OnStateChanged?.Invoke();
        }
    }
}
