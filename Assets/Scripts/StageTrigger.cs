using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageTrigger : MonoBehaviour
{
    [SerializeField] private float endingDelay = 5f;
    [SerializeField] private string endingSceneName = "EndingScene";

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player entered the trigger
        if (other.CompareTag("Player") ||
            other.GetComponentInParent<CharacterController>() != null ||
            other.name.Contains("XR Origin"))
        {
            // Prevent triggering multiple times
            if (hasTriggered)
                return;

            if (MissionManager.Instance != null &&
                MissionManager.Instance.isMuseumOpen)
            {
                hasTriggered = true;

                // Existing logic - trigger NPCs / final stage
                MissionManager.Instance.OnStageReached();

                // Start ending sequence
                StartCoroutine(LoadEndingScene());
            }
        }
    }

    private IEnumerator LoadEndingScene()
    {
        // Wait on the stage for 5 seconds
        yield return new WaitForSeconds(endingDelay);

        // Go to ending scene
        SceneManager.LoadScene(endingSceneName);
    }
}