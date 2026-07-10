using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class FurnitureState : MonoBehaviour
{
    public bool pinned;

    public Rigidbody rb;
    public GrabInteractable grab;
    public HandGrabInteractable handGrab;

    public void ApplyPinned(bool value)
    {
        pinned = value;

        if (rb != null)
        {
            rb.isKinematic = true;   // ’u•¨‚Æ‚µ‚ÄŒÅ’è
            rb.useGravity = false;
        }

        // ’Í‚ß‚È‚­‚·‚é
        if (grab != null) grab.enabled = !pinned;
        if (handGrab != null) handGrab.enabled = !pinned;
    }
}
