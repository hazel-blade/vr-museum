using System.Collections;
using UnityEngine;

public class StorageDoorController : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private Transform doorPivot;

    [Header("Open Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openDuration = 1.5f;

    private bool isKeyUnlocked;
    private bool isChainCut;
    private bool isDoorOpen;

    public void UnlockWithKey()
    {
        if (isKeyUnlocked)
            return;

        isKeyUnlocked = true;
        TryOpenDoor();
    }

    public void CutChain()
    {
        if (isChainCut)
            return;

        isChainCut = true;
        TryOpenDoor();
    }

    private void TryOpenDoor()
    {
        if (isKeyUnlocked && isChainCut && !isDoorOpen)
        {
            StartCoroutine(OpenDoor());
        }
    }

    private IEnumerator OpenDoor()
    {
        isDoorOpen = true;

        Quaternion startRotation = doorPivot.localRotation;

        Quaternion targetRotation =
            startRotation * Quaternion.Euler(0f, openAngle, 0f);

        float elapsedTime = 0f;

        while (elapsedTime < openDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / openDuration
            );

            progress = Mathf.SmoothStep(0f, 1f, progress);

            doorPivot.localRotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                progress
            );

            yield return null;
        }

        doorPivot.localRotation = targetRotation;
    }
}