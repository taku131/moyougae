using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class KeepScaleWhileGrabbed : MonoBehaviour
{
    FurnitureScaleState ss;
    HandGrabInteractable hand;
    GrabInteractable grab;

    void Awake()
    {
        ss = GetComponent<FurnitureScaleState>();
        hand = GetComponent<HandGrabInteractable>();
        grab = GetComponent<GrabInteractable>();
    }

    void LateUpdate()
    {
        if (ss == null) return;

        bool grabbing =
            (hand != null && hand.State == InteractableState.Select) ||
            (grab != null && grab.State == InteractableState.Select);

        if (grabbing)
        {
            transform.localScale = ss.baseScale * ss.multiplier;
        }
    }
}
