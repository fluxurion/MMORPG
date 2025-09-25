using Michsky.MUIP;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MMORPG.Tool
{
    public enum MessageBoxStyle
    {
        LongDesc, // Used to display long messages
        ShortDesc // Used to display short messages, suitable for only two lines of text
    }

    public enum MessageBoxResult
    {
        Confirm, // confirm
        Cancel // Cancel
    }

    /// <summary>
    /// Message box configuration
    /// </summary>
    public class MessageBoxConfig
    {
        public string Title = "Hint";
        public string Description = "i am a message box";
        public string ConfirmButtonText = "Confirm";
        public string CancalButtonText = "Cancel";
        public bool ShowConfirmButton = true;
        public bool ShowCancalButton = false;
        public Action<MessageBoxResult> OnChose = null; // When the user selects one of the "Confirm" or "Cancel" buttons
        public Action OnOpen = null;
        public TaskCompletionSource<MessageBoxResult> OnChoseTcs = null;
        public MessageBoxStyle Style = MessageBoxStyle.LongDesc;
    }

    public class MessageBoxManager : MonoBehaviour
    {
        public ModalWindowManager LongDescModalWindow;
        public ModalWindowManager ShortDescModalWindow;

        public bool IsShowing => GetWindow().isOn;

        public MessageBoxConfig Config { get; set; }

        private void Awake()
        {
            void OnConfirm()
            {
                Config.OnChose?.Invoke(MessageBoxResult.Confirm);
                Config.OnChoseTcs?.TrySetResult(MessageBoxResult.Confirm);
            }

            void OnCancel()
            {
                Config.OnChose?.Invoke(MessageBoxResult.Cancel);
                Config.OnChoseTcs?.TrySetResult(MessageBoxResult.Cancel);
            }

            void OnOpen()
            {
                Config.OnOpen?.Invoke();
            }

            LongDescModalWindow.confirmButton.onClick.AddListener(OnConfirm);
            LongDescModalWindow.cancelButton.onClick.AddListener(OnCancel);
            LongDescModalWindow.onOpen.AddListener(OnOpen);

            ShortDescModalWindow.confirmButton.onClick.AddListener(OnConfirm);
            ShortDescModalWindow.cancelButton.onClick.AddListener(OnCancel);
            ShortDescModalWindow.onOpen.AddListener(OnOpen);
        }

        public void Show()
        {
            if (IsShowing)
                throw new Exception("There is currently a MessageBox being displayed!");

            var window = GetWindow();
            window.titleText = Config.Title;
            window.descriptionText = Config.Description;
            window.confirmButton.buttonText = Config.ConfirmButtonText;
            window.cancelButton.buttonText = Config.CancalButtonText;
            window.showConfirmButton = Config.ShowConfirmButton;
            window.showCancelButton = Config.ShowCancalButton;

            window.cancelButton.UpdateUI();
            window.confirmButton.UpdateUI();
            window.UpdateUI();
            window.Open();
        }

        private ModalWindowManager GetWindow() => Config.Style switch
        {
            MessageBoxStyle.LongDesc => LongDescModalWindow,
            MessageBoxStyle.ShortDesc => ShortDescModalWindow,
            _ => throw new NotImplementedException()
        };
    }
}
