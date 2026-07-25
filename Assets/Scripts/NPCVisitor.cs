using System.Collections;
using UnityEngine;

public class NPCVisitor : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform enterWaypoint;
    public Transform exitWaypoint;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float waitTimeInside = 5f;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void StartVisiting()
    {
        gameObject.SetActive(true);
        StartCoroutine(VisitRoutine());
    }

    private IEnumerator VisitRoutine()
    {
        // Walk to enter waypoint
        if (enterWaypoint != null)
        {
            yield return StartCoroutine(WalkTo(enterWaypoint.position));
        }

        // Wait inside
        if (animator != null) animator.SetBool("IsWalking", false);
        yield return new WaitForSeconds(waitTimeInside);

        // Walk to exit waypoint
        if (exitWaypoint != null)
        {
            yield return StartCoroutine(WalkTo(exitWaypoint.position));
        }

        // Deactivate or destroy upon exit
        gameObject.SetActive(false);
    }

    private IEnumerator WalkTo(Vector3 targetPosition)
    {
        if (animator != null) animator.SetBool("IsWalking", true);

        // Face the target
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        }

        while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(targetPosition.x, 0, targetPosition.z)) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
    }
}
