using UnityEngine;
using UnityEngine.Events;

public class MissionManagerExtension : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onMuseumRestored;

    private MissionManager missionManager;
    private bool wasOpen = false;

    private void Start()
    {
        missionManager = MissionManager.Instance;
        if (missionManager == null)
        {
            missionManager = Object.FindObjectOfType<MissionManager>();
        }
    }

    private void Update()
    {
        if (missionManager != null)
        {
            if (missionManager.isMuseumOpen && !wasOpen)
            {
                wasOpen = true;
                onMuseumRestored?.Invoke();
            }
        }
    }
}
