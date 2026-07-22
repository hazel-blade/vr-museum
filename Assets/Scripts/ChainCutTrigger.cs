using UnityEngine;

public class ChainCutTrigger : MonoBehaviour
{
    [SerializeField] private StorageDoorController doorController;
    [SerializeField] private GameObject chainObject;

    private bool isCut;

    private void OnTriggerEnter(Collider other)
    {
        if (isCut)
            return;

        if (!other.CompareTag("BoltCutter"))
            return;

        isCut = true;

        doorController.CutChain();

        if (chainObject != null)
        {
            chainObject.SetActive(false);
        }

        Debug.Log("Chain has been cut.");
    }
}