using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InputFocusHandler : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    // Assign the field this input should jump to when TAB is pressed
    public InputField nextInCycleField;

    private InputField currentInputField;
    private bool isSelected = false;

    // ADDED: Timer to prevent instant cycling
    private float lastSelectTime;
    private const float CooldownDuration = 0.5f; // Ignore Tab for 0.5 seconds after select

    void Start()
    {
        currentInputField = GetComponent<InputField>();

        if (nextInCycleField == null)
        {
            Debug.LogError("InputFocusHandler on " + gameObject.name + " is missing the reference to the nextInCycleField!");
        }

        // Ensure built-in navigation is off
        Navigation nav = currentInputField.navigation;
        nav.mode = Navigation.Mode.None;
        currentInputField.navigation = nav;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        // RESET THE TIMER: Set the time this field gained focus
        lastSelectTime = Time.time;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }

    void Update()
    {
        if (isSelected)
        {
            // CHECK 1: Ensure enough time has passed since this field gained focus
            if (Time.time < lastSelectTime + CooldownDuration)
            {
                return; // Ignore input during the cooldown period
            }

            // CHECK 2: Check ONLY for the Tab key press
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                // Ensure Shift is NOT held for cycling
                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    FocusField(nextInCycleField);
                }
            }
        }
    }

    private void FocusField(InputField targetField)
    {
        if (targetField != null)
        {
            targetField.Select();
            EventSystem.current.SetSelectedGameObject(targetField.gameObject);
        }
    }
}
