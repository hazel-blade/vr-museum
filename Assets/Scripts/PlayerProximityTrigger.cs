using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class PlayerProximityTrigger : MonoBehaviour
{
    [Tooltip("Fired when the player enters the trigger")]
    public UnityEvent onPlayerEnter;

    [Tooltip("Fired when the player exits the trigger")]
    public UnityEvent onPlayerExit;

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            onPlayerEnter?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            onPlayerExit?.Invoke();
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || 
               other.GetComponentInParent<CharacterController>() != null || 
               other.name.Contains("XR Origin");
    }
}
