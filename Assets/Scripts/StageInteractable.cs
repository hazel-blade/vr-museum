using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRSimpleInteractable))]
public class StageInteractable : MonoBehaviour
{
    private bool isCompleted = false;

    private void Awake()
    {
        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        // Trigger if the player touches it with their VR hand (Hover) or grabs it (Select)
        interactable.hoverEntered.AddListener(OnInteract);
        interactable.selectEntered.AddListener(OnInteract);
    }

    private void OnInteract(HoverEnterEventArgs args) => CompleteMission();
    private void OnInteract(SelectEnterEventArgs args) => CompleteMission();

    private void OnTriggerEnter(Collider other)
    {
        // Trigger if the player's body or hand simply walks into it physically
        // We ignore NPCs so they don't accidentally trigger it
        if (!other.name.Contains("NPC") && !other.CompareTag("NPC"))
        {
            CompleteMission();
        }
    }

    private void CompleteMission()
    {
        if (isCompleted) return;

        if (MissionManager.Instance != null && MissionManager.Instance.isMuseumOpen)
        {
            isCompleted = true;
            MissionManager.Instance.OnStageReached();
            
            // Disable this interactable so it can't be triggered again
            XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
            if (interactable != null) interactable.enabled = false;
        }
        else
        {
            Debug.Log("Mission 3 not active yet! Complete Missions 1 & 2 first.");
        }
    }
}
