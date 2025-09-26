using UnityEngine;
using UnityEngine.UI; // Required for InputField
using UnityEngine.EventSystems; // Required for EventSystem

public class LoginAutoSelect : MonoBehaviour
{
    // Assign your Username InputField in the Inspector
    public InputField usernameInput;

    void Start()
    {
        // 1. Check if the InputField is assigned and if it's currently interactable
        if (usernameInput != null && usernameInput.interactable)
        {
            // 2. Select the InputField
            usernameInput.Select();

            // 3. (Optional but recommended) Set it as the currently selected GameObject in the EventSystem
            // This ensures the cursor is placed and active immediately.
            EventSystem.current.SetSelectedGameObject(usernameInput.gameObject);
        }
    }
}
