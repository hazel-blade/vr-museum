using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light flashlightLight;
    [SerializeField] private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }

        if (flashlightLight == null)
        {
            Debug.LogError(
                "FlashlightLight reference is missing.",
                this
            );

            return;
        }

        flashlightLight.enabled = false;

        Debug.Log("Flashlight controller ready.", this);
    }

    private void OnEnable()
    {
        if (grabInteractable == null)
        {
            Debug.LogError(
                "XRGrabInteractable was not found.",
                this
            );

            return;
        }

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        if (grabInteractable == null)
            return;

        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log("Flashlight grabbed.", this);

        if (flashlightLight != null)
        {
            flashlightLight.enabled = true;
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        Debug.Log("Flashlight released.", this);

        if (flashlightLight != null)
        {
            flashlightLight.enabled = false;
        }
    }
}