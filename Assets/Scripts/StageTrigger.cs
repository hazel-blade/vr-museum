using UnityEngine;

public class StageTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the player entered the trigger
        if (other.CompareTag("Player") || other.GetComponentInParent<CharacterController>() != null || other.name.Contains("XR Origin"))
        {
            if (MissionManager.Instance != null && MissionManager.Instance.isMuseumOpen)
            {
                MissionManager.Instance.OnStageReached();
            }
        }
    }
}
