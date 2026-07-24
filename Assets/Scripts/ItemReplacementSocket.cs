using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ItemReplacementSocket : MonoBehaviour
{
    [Header("Mission Settings")]
    [SerializeField] private bool isItem1 = true; // true = Mission 2 (Nodachi), false = Mission 3 (Saya_Nodachi)
    [SerializeField] private string requiredTag = "Relic";

    [Tooltip("Type the exact name of the item. E.g. 'Nodachi' for Table 2, 'Saya_Nodachi' for Table 3.")]
    [SerializeField] private string requiredObjectName = "";

    private bool isActivated = false;

    private void Awake()
    {
        XRSocketInteractor socket = GetComponent<XRSocketInteractor>();
        if (socket != null)
        {
            socket.selectEntered.AddListener(OnItemInserted);
        }
    }

    public void OnItemInserted(SelectEnterEventArgs args)
    {
        GameObject insertedObject = args.interactableObject.transform.gameObject;

        Debug.Log($"SOCKET: '{insertedObject.name}' (tag='{insertedObject.tag}') placed in '{gameObject.name}'. Looking for tag='{requiredTag}', name='{requiredObjectName}'.");

        if (isActivated) return;

        // 1. Check tag or name match
        bool tagMatches = insertedObject.CompareTag(requiredTag) || insertedObject.transform.root.CompareTag(requiredTag);
        bool nameMatches = !string.IsNullOrEmpty(requiredObjectName) && 
                           (insertedObject.name.Contains(requiredObjectName) || insertedObject.transform.root.name.Contains(requiredObjectName));

        if (!tagMatches && !nameMatches)
        {
            Debug.Log($"WRONG ITEM: Expected tag '{requiredTag}' or name containing '{requiredObjectName}', but got '{insertedObject.name}' (tag: '{insertedObject.tag}').");
            return;
        }

        ActivateMissionItem();
    }

    private void ActivateMissionItem()
    {
        isActivated = true;
        Debug.Log($"CORRECT ITEM placed! Completing mission {(isItem1 ? "2" : "3")}.");

        if (MissionManager.Instance != null)
        {
            if (isItem1)
                MissionManager.Instance.CompleteItem1();
            else
                MissionManager.Instance.CompleteItem2();
        }
        else
        {
            Debug.LogError("MissionManager not found in scene!");
        }
    }
}
