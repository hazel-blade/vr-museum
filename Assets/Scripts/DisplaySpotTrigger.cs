using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Attach this to a spot on the floor near a museum display item.
/// When the player walks onto the spot, it shows the linked InfoCanvas.
/// When the player walks off, it hides the InfoCanvas.
///
/// Player detection: Tag "Player", CharacterController, or name containing "XR Origin".
/// NPCs are ignored so they don't accidentally trigger info panels.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRSimpleInteractable))]
public class DisplaySpotTrigger : MonoBehaviour
{
    [Tooltip("The InfoCanvas GameObject to show/hide when the player stands on this spot.")]
    public GameObject infoCanvas;

    private void Awake()
    {
        // Rigidbody is REQUIRED for OnTriggerEnter/Exit to fire in Unity.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // XR Support: If the player points their VR ray at the spot, or their hand touches it
        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener((args) => ShowInfo("XR Hover"));
            // Removed hoverExited so the info stays open when you look away
        }
    }

    private void Start()
    {
        if (infoCanvas != null)
        {
            UIManager uiMgr = infoCanvas.GetComponent<UIManager>();
            if (uiMgr != null)
            {
                // If using UIManager, the root must stay active while the inner UICanvas is hidden
                infoCanvas.SetActive(true);
                uiMgr.HideUI();
            }
            else
            {
                // Fallback for simple canvases
                infoCanvas.SetActive(false);
            }
        }
    }

    private void ShowInfo(string source)
    {
        if (infoCanvas != null)
        {
            UIManager uiMgr = infoCanvas.GetComponent<UIManager>();
            if (uiMgr != null)
                uiMgr.ShowUI();
            else
                infoCanvas.SetActive(true);

            Debug.Log($"[DisplaySpotTrigger] {source} triggered Show on {gameObject.name}");
        }
    }

    private void HideInfo(string source)
    {
        if (infoCanvas != null)
        {
            UIManager uiMgr = infoCanvas.GetComponent<UIManager>();
            if (uiMgr != null)
                uiMgr.HideUI();
            else
                infoCanvas.SetActive(false);

            Debug.Log($"[DisplaySpotTrigger] {source} triggered Hide on {gameObject.name}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore NPCs, but accept literally anything else (hands, body, etc) just like StageInteractable
        if (other.gameObject.tag == "NPC" || other.name.Contains("NPC"))
            return;

        ShowInfo("Physics");
    }

    // Removed OnTriggerExit so the canvas stays open when the player walks away.
    // The player can close it manually using the UI Close button.
}
