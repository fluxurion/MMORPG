using UnityEngine;
using UnityEngine.UI; // For InputField and Toggle

public class LoginDataManager : MonoBehaviour
{
    public static LoginDataManager Instance { get; private set; }

    // Assign these in the Inspector
    public InputField usernameInput;
    public InputField passwordInput;
    public Toggle saveUsernameToggle;
    public Toggle savePasswordToggle;

    // Keys for PlayerPrefs
    private const string UsernameKey = "SavedUsername";
    private const string PasswordKey = "SavedPassword";
    private const string SaveUsernameToggleKey = "SaveUsernameChecked";
    private const string SavePasswordToggleKey = "SavePasswordChecked";

    private void Awake()
    {
        // Enforce the Singleton pattern
        if (Instance != null && Instance != this)
        {
            // If another instance exists, destroy this one
            Destroy(this.gameObject);
        }
        else
        {
            // Set this instance as the Singleton
            Instance = this;
            // (Optional) Keep this object alive across scene loads
            // DontDestroyOnLoad(this.gameObject); 
        }
    }

    void Start()
    {
        // --- 1. Load the previous state when the scene starts ---
        LoadCredentials();
    }

    // --- 2. Saving Function (Call this on Login or when the game quits) ---
    public void SaveCredentials()
    {
        // Save the state of the Toggles
        PlayerPrefs.SetInt(SaveUsernameToggleKey, saveUsernameToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt(SavePasswordToggleKey, savePasswordToggle.isOn ? 1 : 0);

        // Save Username if the toggle is checked
        if (saveUsernameToggle.isOn)
        {
            PlayerPrefs.SetString(UsernameKey, usernameInput.text);
        }
        else
        {
            // If unchecked, clear the saved value
            PlayerPrefs.DeleteKey(UsernameKey);
        }

        // Save Password if the toggle is checked
        if (savePasswordToggle.isOn)
        {
            // NOTE: Saving passwords in PlayerPrefs is INSECURE! 
            // This is for demonstration. For a real game, use encryption or a server.
            PlayerPrefs.SetString(PasswordKey, passwordInput.text);
        }
        else
        {
            // If unchecked, clear the saved value
            PlayerPrefs.DeleteKey(PasswordKey);
        }

        // Ensure the data is written to disk immediately
        PlayerPrefs.Save();
    }

    // --- 3. Loading Function (Called in Start) ---
    private void LoadCredentials()
    {
        // Load the toggle states
        bool saveUser = PlayerPrefs.GetInt(SaveUsernameToggleKey, 0) == 1; // Default to 0 (false)
        bool savePass = PlayerPrefs.GetInt(SavePasswordToggleKey, 0) == 1;

        saveUsernameToggle.isOn = saveUser;
        savePasswordToggle.isOn = savePass;

        // Load Username if saved
        if (saveUser && PlayerPrefs.HasKey(UsernameKey))
        {
            usernameInput.text = PlayerPrefs.GetString(UsernameKey);
        }
        else
        {
            usernameInput.text = ""; // Clear if the toggle was unchecked
        }

        // Load Password if saved
        if (savePass && PlayerPrefs.HasKey(PasswordKey))
        {
            passwordInput.text = PlayerPrefs.GetString(PasswordKey);
        }
        else
        {
            passwordInput.text = ""; // Clear if the toggle was unchecked
        }
    }

    // --- 4. Special Unity function to save when the application closes ---
    private void OnApplicationQuit()
    {
        SaveCredentials();
    }
}
