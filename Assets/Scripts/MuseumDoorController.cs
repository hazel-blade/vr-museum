using System.Collections;
using UnityEngine;

public class MuseumDoorController : MonoBehaviour
{
    [Header("Door Visuals")]
    [SerializeField] private Transform doorPivot;

    [Header("Animation Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openDuration = 1.5f;

    private bool isDoorOpen = false;

    public void OpenDoor()
    {
        if (isDoorOpen) return;
        
        if (doorPivot != null)
        {
            StartCoroutine(AnimateDoorOpen());
        }
        else
        {
            Debug.LogWarning("MuseumDoorController: Door Pivot is not assigned!");
        }
    }

    private IEnumerator AnimateDoorOpen()
    {
        isDoorOpen = true;
        Quaternion startRotation = doorPivot.localRotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, openAngle, 0f);
        
        float elapsedTime = 0f;
        while (elapsedTime < openDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsedTime / openDuration);
            doorPivot.localRotation = Quaternion.Slerp(startRotation, targetRotation, progress);
            yield return null;
        }

        doorPivot.localRotation = targetRotation;
    }
}
