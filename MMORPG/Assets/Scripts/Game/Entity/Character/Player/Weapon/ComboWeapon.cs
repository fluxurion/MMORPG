using System;
using System.Linq;
using MMORPG.Event;
using MMORPG.Tool;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MMORPG.Game
{
    public class ComboWeapon : MonoBehaviour, IController, ICanSendEvent
    {
        [Tooltip("Start combo switching")]
        public bool DroppableCombo = true;
        [Tooltip("Combo judgment interval, if exceeded, switch back to the first combo")]
        public float DropComboDelay = 1f;
        [Tooltip("Cooldown time after completing a combo")]
        public float ComboCoolTime = 1f;

        [Information("Automatically obtain all weapons on the current object", GUIAlwaysEnabled = true)]
        [Information("Note: Do not activate each weapon attached\"InitializeOnStart\", Because initialization is controlled by ComboWeapon!", InfoMessageType.Warning, GUIAlwaysEnabled = true)]
        [ReadOnly]
        public Weapon[] Weapons;

        public int CurrentWeaponIndex { get; private set; }

        public Weapon CurrentWeapon => Weapons[CurrentWeaponIndex];

        public PlayerHandleWeapon Owner { get; private set; }

        private float _timeSinceComboFinished = -float.MaxValue;
        private float _timeSinceLastWeaponStopped = -float.MaxValue;
        private bool _inComboCooling;
        private bool _inCombo;

        private void Awake()
        {
            Weapons.ForEach(x =>
            {
                if (x.InitializeOnStart)
                    throw new Exception("Do not make \"InitializeOnStart\" true on any Weapon!");
            });
        }

        private void Start()
        {
            Weapons = GetComponents<Weapon>();
            Weapons.ForEach(x =>
            {
                x.OnWeaponStarted += OnWeaponStarted;
                x.OnWeaponStopped += OnWeaponStopped;
            });
            Owner = Weapons[0].HandleWeapon;
            Debug.Assert(Owner != null);
        }

        private void Update()
        {
            ResetCombo();
        }

        protected virtual void ResetCombo()
        {
            if (Weapons.Length > 1)
            {
                if (_inCombo && DroppableCombo && Time.time - _timeSinceLastWeaponStopped > DropComboDelay)
                {
                    _inCombo = false;
                    CurrentWeaponIndex = 0;
                    Owner.ChangeWeapon(CurrentWeapon, true);
                }
            }

            if (_inComboCooling && Time.time - _timeSinceComboFinished > ComboCoolTime)
            {
                Weapons[0].PreventFire = false;
                _inComboCooling = false;
            }
        }

        protected virtual void OnWeaponStarted(Weapon weapon)
        {
        }

        protected virtual void OnWeaponStopped(Weapon weapon)
        {
            ProceedToNextCombo();
        }

        protected virtual void ProceedToNextCombo()
        {
            Debug.Assert(Owner == CurrentWeapon.HandleWeapon);

            if (Weapons.Length > 1)
            {
                var newIndex = 0;
                if (CurrentWeaponIndex < Weapons.Length - 1)
                {
                    newIndex = CurrentWeaponIndex + 1;
                    _inCombo = true;
                }
                else
                {
                    _timeSinceComboFinished = Time.time;
                    Weapons[0].PreventFire = true;
                    _inComboCooling = true;
                    _inCombo = false;
                }

                _timeSinceLastWeaponStopped = Time.time;

                CurrentWeaponIndex = newIndex;
                Owner.ChangeWeapon(CurrentWeapon, true);
            }
        }

        public void ChangeCombo(int weaponId)
        {
            int index = 0;
            for (; index < Weapons.Length; index++)
            {
                if (Weapons[index].WeaponId == weaponId)
                    break;
            }

            CurrentWeaponIndex = index;
            Owner.ChangeWeapon(CurrentWeapon, true);
        }

        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }
    }
}
