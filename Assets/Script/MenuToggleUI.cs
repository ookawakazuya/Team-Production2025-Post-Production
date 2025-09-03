using UnityEngine;
using UnityEngine.InputSystem;

public class MenuToggleUI : MonoBehaviour
{
    //’S“–Š™“c

    [Header("Input Action (Menu Button)")]
    public InputActionProperty menuAction;

    [Header("UI Root Object")]
    public GameObject uiCanvas;

    void OnEnable()
    {
        menuAction.action.performed += OnMenuPressed;
        menuAction.action.Enable();
    }

    void OnDisable()
    {
        menuAction.action.performed -= OnMenuPressed;
        menuAction.action.Disable();
    }

    private void OnMenuPressed(InputAction.CallbackContext ctx)
    {
        if (uiCanvas != null)
        {
            uiCanvas.SetActive(!uiCanvas.activeSelf);
        }
    }
}
