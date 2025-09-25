using TMPro;
using UnityEngine;

namespace MMORPG.Tool
{
    /// <summary>
    /// Configuration of the rotating loading frame
    /// </summary>
    public record SpinnerBoxConfig
    {
        public string Description = "I am a rotating loading panel";
        public float DescriptionFontSize = 16;
    }

    public class SpinnerBoxManager : MonoBehaviour
    {
        public GameObject SpinnerBox;
        public TextMeshProUGUI DescriptionText;

        public SpinnerBoxConfig Config { get; set; }

        public bool IsShowing { get; private set; }

        private void Start()
        {
            SpinnerBox.SetActive(false);
        }

        public void Show()
        {
            if (IsShowing)
            {
                Debug.LogWarning("The current SpinnerBox is showing!");
                return;
            }

            DescriptionText.text = Config.Description;
            DescriptionText.fontSize = Config.DescriptionFontSize;
            IsShowing = true;
            PanelHelper.FadeIn(SpinnerBox);
        }

        public void Close()
        {
            Debug.Assert(IsShowing);
            PanelHelper.FadeOut(SpinnerBox);
            IsShowing = false;
        }
    }
}
