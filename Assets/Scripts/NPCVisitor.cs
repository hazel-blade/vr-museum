using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCVisitor : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform enterWaypoint;
    public Transform[] museumWaypoints;
    public Transform exitWaypoint;

    [Header("Movement Settings")]
    public float moveSpeed = 0.8f;
    public float waitTimeInside = 5f;

    private Animator animator;
    private NavMeshAgent agent;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        
        if (agent != null)
        {
            agent.speed = moveSpeed;
            // Lower avoidance quality so they don't stutter when crossing paths
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }
    }

    public void StartVisiting()
    {
        gameObject.SetActive(true);
        StartCoroutine(VisitRoutine());
    }

    private IEnumerator VisitRoutine()
    {
        Vector3 startPos = transform.position; // Remember initial spawn point outside door 2

        while (true)
        {
            // Teleport back to spawn point for the next loop
            if (agent != null) agent.Warp(startPos);
            else transform.position = startPos;

            // Walk to enter waypoint
            if (enterWaypoint != null)
            {
                yield return StartCoroutine(WalkTo(enterWaypoint.position));
            }

            // Walk around museum waypoints
            if (museumWaypoints != null && museumWaypoints.Length > 0)
            {
                foreach (var wp in museumWaypoints)
                {
                    if (wp != null)
                    {
                        yield return StartCoroutine(WalkTo(wp.position));
                        yield return new WaitForSeconds(waitTimeInside);
                    }
                }
            }
            else
            {
                // Wait inside fallback
                if (animator != null) animator.SetBool("IsWalking", false);
                yield return new WaitForSeconds(waitTimeInside);
            }

            // Walk to exit waypoint
            if (exitWaypoint != null)
            {
                yield return StartCoroutine(WalkTo(exitWaypoint.position));
            }

            // Briefly stop at the exit before looping
            if (animator != null) animator.SetBool("IsWalking", false);
            yield return new WaitForSeconds(2f);
        }
    }

    private IEnumerator WalkTo(Vector3 targetPosition)
    {
        if (agent == null) yield break;

        if (animator != null) animator.SetBool("IsWalking", true);

        agent.SetDestination(targetPosition);

        // Wait for path calculation
        while (agent.pathPending)
        {
            yield return null;
        }

        // Wait until we reach the destination
        while (agent.remainingDistance > agent.stoppingDistance + 0.1f)
        {
            if (animator != null) animator.SetBool("IsWalking", true);
            yield return null;
        }

        if (animator != null) animator.SetBool("IsWalking", false);
    }
}
