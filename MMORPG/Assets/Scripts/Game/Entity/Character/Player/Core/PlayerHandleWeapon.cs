using MMORPG.Event;
using MMORPG.Tool;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MMORPG.Game
{
    public class PlayerHandleWeapon : MonoBehaviour
    {
        [Title("Weapon")]
        [AssetsOnly]
        [Tooltip("Weapon held at initialization")]
        public Weapon InitialWeapon;

        [Title("Binding")]
        [Tooltip("Weapon attachment position")]
        public Transform WeaponAttachment;

        public Weapon CurrentWeapon { get; private set; }
        public ComboWeapon CurrentComboWeapon { get; private set; }

        public delegate void WeaponChangedHandler(Weapon current, Weapon previous);
        public event WeaponChangedHandler OnWeaponChanged;

        public PlayerBrain Brain { get; private set; }

        private void Start()
        {
            if (InitialWeapon)
            {
                ChangeWeapon(InitialWeapon);
            }
        }

        private void Update()
        {
        }

        public void Setup(PlayerBrain owner)
        {
            Brain = owner;
        }

        /// <summary>
        /// Change weapons held
        /// </summary>
        /// <param name="newWeapon"></param>
        /// <param name="combo">Is the current weapon only for combo switching?</param>
        public void ChangeWeapon(Weapon newWeapon, bool combo = false)
        {
            if (CurrentWeapon)
            {
                CurrentWeapon.TurnWeaponOff();
                if (!combo)
                {
                    Destroy(CurrentWeapon.gameObject);
                }
            }

            var tmp = CurrentWeapon;
            if (newWeapon != null)
            {
                InstantiateWeapon(newWeapon, combo);
            }
            else
            {
                CurrentWeapon = null;
            }
            OnWeaponChanged?.Invoke(newWeapon, tmp);
        }

        private void InstantiateWeapon(Weapon newWeapon, bool combo)
        {
            if (!combo)
            {
                CurrentWeapon = Instantiate(newWeapon);

                // If it is not Mine, turn off the Combo switch
                if (CurrentWeapon.TryGetComponent(out ComboWeapon comboWeapon))
                {
                    if (!Brain.IsMine)
                        comboWeapon.DroppableCombo = false;
                }

                CurrentComboWeapon = comboWeapon;
            }
            else
            {
                CurrentWeapon = newWeapon;
            }
            CurrentWeapon.transform.SetParent(WeaponAttachment, false);
            CurrentWeapon.Setup(this);
            if (!CurrentWeapon.InitializeOnStart)
                CurrentWeapon.Initialize();
        }

        /// <summary>
        /// use weapons
        /// </summary>
        public void ShootStart()
        {
            if (CurrentWeapon == null)
            {
                return;
            }
            CurrentWeapon.WeaponInputStart();
        }
    }
}
