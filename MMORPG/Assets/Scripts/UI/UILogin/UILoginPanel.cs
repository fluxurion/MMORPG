using MMORPG.Command;
using MMORPG.Game;
using QFramework;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using System;

namespace MMORPG.UI
{
    public class UILoginPanelData : UIPanelData
    {
    }

    public partial class UILoginPanel : UIPanel, IController
    {
        private LoginDataManager m_LoginDataManager;

        // Defines the core action that happens on login
        private Action m_LoginAction;

        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }

        private void Awake()
        {
            LoginDataManager dataManager = LoginDataManager.Instance;

            // --- DEFINE LOGIN ACTION ---
            m_LoginAction = () =>
            {
                if (dataManager != null)
                {
                    dataManager.SaveCredentials();
                }

                this.SendCommand(new LoginCommand(
                    InputUsername.text,
                    InputPassword.text));
            };

            // --- 2. INPUT EVENT LISTENERS ---

            // Login Button Click
            BtnLogin.onClick.AddListener(() => m_LoginAction());

            // Enter Key on Password Field (Triggers Login)
            InputPassword.onEndEdit.AddListener((value) =>
            {
                // Check if the "Enter" key was the cause of the end edit
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    m_LoginAction();
                }
            });

            // Enter Key on Username Field (Triggers Login)
            InputUsername.onEndEdit.AddListener((value) =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    m_LoginAction();
                }
            });
        }

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as UILoginPanelData ?? new UILoginPanelData();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            // --- AUTO-SELECT USERNAME ON OPEN ---
            if (InputUsername != null && InputUsername.interactable)
            {
                InputUsername.Select();
                EventSystem.current.SetSelectedGameObject(InputUsername.gameObject);
            }
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
        }
    }
}
