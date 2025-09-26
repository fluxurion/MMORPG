using UnityEngine;
using UnityEngine.UI;

namespace MMORPG.UI
{
    /// <summary>
    /// Fullscreen background image proportionally scaled and adapted
    /// </summary>
    [ExecuteInEditMode]
    public class UIBackgroundAdjuster : MonoBehaviour
    {
        // Original image size (before compression)
        //public Vector2 textureOriginSize = new Vector2(1376, 920);

        private RectTransform rt;
        private Image bgImage;
        private Canvas canvas;

        void OnEnable()
        {
            Initialize();
            Scaler();
        }

        // Initialize component
        void Initialize()
        {
            rt = GetComponent<RectTransform>();
            bgImage = GetComponent<Image>();
            canvas = GetComponentInParent<Canvas>();
        }

        // Adaptation
        void Scaler()
        {
            if (canvas == null || bgImage == null)
            {
                // Debug.LogError("Canvas or Image component not found.");
                return;
            }

            // Current canvas size
            Vector2 canvasSize = canvas.GetComponent<RectTransform>().sizeDelta;
            // Current canvas size aspect ratio
            float screenxyRate = canvasSize.x / canvasSize.y;

            // Image size
            float textureWidth = bgImage.mainTexture.width;
            float textureHeight = bgImage.mainTexture.height;
            // Video size aspect ratio
            float texturexyRate = textureWidth / textureHeight;

            // Video x is too long, needs to be adjusted to y (change '>' to '<' in the following judgment to match the video player's video mode)
            if (texturexyRate > screenxyRate)
            {
                int newSizeY = Mathf.CeilToInt(canvasSize.y);
                int newSizeX = Mathf.CeilToInt((float)newSizeY / textureHeight * textureWidth);
                rt.sizeDelta = new Vector2(newSizeX, newSizeY);
            }
            else
            {
                int newVideoSizeX = Mathf.CeilToInt(canvasSize.x);
                int newVideoSizeY = Mathf.CeilToInt((float)newVideoSizeX / textureWidth * textureHeight);
                rt.sizeDelta = new Vector2(newVideoSizeX, newVideoSizeY);
            }
        }

        // Resize the background image when the window size changes
        void OnRectTransformDimensionsChange()
        {
            Scaler();
        }
    }
}
