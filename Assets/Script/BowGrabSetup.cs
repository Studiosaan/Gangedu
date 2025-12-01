using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BowGrabSetup : MonoBehaviour
{
    private Rigidbody rb;
    private XRGrabInteractable grab;

    public void Initialize(Rigidbody _rb, XRGrabInteractable _grab)
    {
        rb = _rb ?? GetComponent<Rigidbody>();
        grab = _grab ?? GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            grab.selectExited.AddListener(OnReleased);
            grab.selectEntered.AddListener(OnGrabbed);
        }
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    private void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectExited.RemoveListener(OnReleased);
            grab.selectEntered.RemoveListener(OnGrabbed);
        }
    }
}