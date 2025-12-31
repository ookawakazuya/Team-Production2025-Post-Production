using UnityEngine;
using UnityEngine.InputSystem;

public class RightClickPlaySound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip clip;

    void Update()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
