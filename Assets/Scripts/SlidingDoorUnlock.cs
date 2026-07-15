using System.Collections;
using UnityEngine;

public class SlidingDoorUnlock : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private Transform door;

    [Header("Movement")]
    [SerializeField] private Vector3 slideOffset = new Vector3(2f, 0f, 0f);
    [SerializeField] private float moveDuration = 1.5f;

    [Header("Key")]
    [SerializeField] private string keyTag = "Key";
    [SerializeField] private bool destroyKeyAfterUnlock = false;

    private bool isUnlocked;
    private Vector3 closedPosition;
    private Vector3 openPosition;

    private void Awake()
    {
        if (door == null)
        {
            Debug.LogError("Door reference is missing.", this);
            enabled = false;
            return;
        }

        closedPosition = door.localPosition;
        openPosition = closedPosition + slideOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isUnlocked)
            return;

        if (!other.CompareTag(keyTag))
            return;

        isUnlocked = true;

        StartCoroutine(OpenDoor());

        if (destroyKeyAfterUnlock)
        {
            Destroy(other.gameObject);
        }
    }

    private IEnumerator OpenDoor()
    {
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / moveDuration);
            progress = Mathf.SmoothStep(0f, 1f, progress);

            door.localPosition = Vector3.Lerp(
                closedPosition,
                openPosition,
                progress
            );

            yield return null;
        }

        door.localPosition = openPosition;
    }
}