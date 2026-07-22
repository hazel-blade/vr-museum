using UnityEngine;

public class KeyLockTrigger : MonoBehaviour
{
    [SerializeField] private StorageDoorController doorController;
    [SerializeField] private GameObject padlockObject;

    private bool isUnlocked;

    private void OnTriggerEnter(Collider other)
    {
        if (isUnlocked)
            return;

        if (!other.CompareTag("StorageKey"))
            return;

        isUnlocked = true;

        if (padlockObject != null)
        {
            padlockObject.SetActive(false);
        }

        doorController.UnlockWithKey();

        Debug.Log("Correct key used.");
    }
}