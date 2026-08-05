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
    public float maxWalkTimeout = 30f;
    public int loopsBeforeExit = 2;

    [Header("Foot Alignment & Height")]
    [Tooltip("Adjust in Inspector during play mode to plant shoes accurately without floating or digging (e.g. 0.03 to raise, -0.03 to lower).")]
    public float baseHeightOffset = 0.0f;

    private Animator animator;
    private NavMeshAgent agent;

    // Animation states and parameters
    private string walkStateName = "";
    private string idleStateName = "";
    private bool hasWalkingParam = false;
    private string walkingParamName = "IsWalking";
    private bool isCurrentlyWalking = false;
    private bool animationInitialized = false;
    private float initialFloorY;

    private void Awake()
    {
        initialFloorY = transform.position.y;
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.2f;
            // Lower avoidance quality so they don't stutter when crossing paths
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }

        InitializeAnimation();
    }

    private void OnEnable()
    {
        InitializeAnimation();
        if (animator != null)
        {
            isCurrentlyWalking = false;
            ApplyAnimationState(false);
        }
    }

    private void InitializeAnimation()
    {
        if (animator == null || animationInitialized) return;

        // First, check if the Animator Controller explicitly defines a boolean parameter (e.g. IsWalking, Walk, Moving)
        if (animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Bool)
                {
                    string lower = param.name.ToLower();
                    if (lower.Contains("walk") || lower.Contains("move") || lower.Contains("run"))
                    {
                        hasWalkingParam = true;
                        walkingParamName = param.name;
                        break;
                    }
                }
            }

            // If no parameters or transitions are configured (default in DenysAlmaral CityPeople pack), scan clip names
            if (!hasWalkingParam && animator.runtimeAnimatorController.animationClips != null)
            {
                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                {
                    string lowerName = clip.name.ToLower();
                    if (string.IsNullOrEmpty(walkStateName) && (lowerName.Contains("walk") || lowerName.Contains("jog") || lowerName.Contains("locom") || lowerName.Contains("forward")))
                    {
                        walkStateName = clip.name;
                    }
                    else if (string.IsNullOrEmpty(idleStateName) && (lowerName.Contains("idle") || lowerName.Contains("stand") || lowerName.Contains("check")))
                    {
                        idleStateName = clip.name;
                    }
                }
            }

            // Fallback default names if animation clip names did not match standard patterns
            if (string.IsNullOrEmpty(walkStateName)) walkStateName = "locom_m_basicWalk_30f";
            if (string.IsNullOrEmpty(idleStateName)) idleStateName = "idle_m_2_220f";
        }

        animationInitialized = true;
    }

    private void Update()
    {
        if (animator == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        // Safely adjust shoe height directly via baseOffset without relying on physics raycasts against colliderless decorative floors
        if (agent.baseOffset != baseHeightOffset)
        {
            agent.baseOffset = baseHeightOffset;
        }

        // Anti-clipping safety guard: immediately restore position if pushed or pathing underneath the museum floor
        if (transform.position.y < initialFloorY - 0.4f)
        {
            Debug.LogWarning($"[{gameObject.name}] Sunk beneath museum floor! Restoring to valid floor elevation.");
            Vector3 recoveredPos = new Vector3(transform.position.x, initialFloorY, transform.position.z);
            if (NavMesh.SamplePosition(recoveredPos, out NavMeshHit floorHit, 1.5f, NavMesh.AllAreas))
            {
                agent.Warp(floorHit.position);
            }
            else
            {
                agent.Warp(recoveredPos);
            }
            return;
        }

        // Monitor physical velocity on the ground plane to dynamically transition between walking and idle
        bool isMoving = agent.velocity.sqrMagnitude > 0.005f && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance);

        if (isMoving != isCurrentlyWalking)
        {
            isCurrentlyWalking = isMoving;
            ApplyAnimationState(isMoving);
        }
    }

    private void ApplyAnimationState(bool walking)
    {
        if (animator == null) return;

        if (hasWalkingParam)
        {
            animator.SetBool(walkingParamName, walking);
        }
        else
        {
            string targetState = walking ? walkStateName : idleStateName;
            if (!string.IsNullOrEmpty(targetState))
            {
                // Smoothly transition into the desired animation state in 0.25 seconds
                animator.CrossFadeInFixedTime(targetState, 0.25f);
            }
        }
    }

    public void StartVisiting()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(VisitRoutine());
    }

    private IEnumerator VisitRoutine()
    {
        Vector3 startPos = transform.position; // Remember initial spawn point outside door 2

        // Wait one frame after SetActive(true) for NavMeshAgent to bind to the NavMesh
        yield return null;

        while (true)
        {
            // Sample nearest valid NavMesh position with a tight radius (1.5m) to prevent grabbing underneath terrain
            if (NavMesh.SamplePosition(startPos, out NavMeshHit startHit, 1.5f, NavMesh.AllAreas))
            {
                startPos = startHit.position;
            }

            // Teleport back to spawn point for the next loop
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.Warp(startPos);
            }
            else
            {
                transform.position = startPos;
            }

            // Wait a brief frame after warping before setting a new destination
            yield return null;

            // Walk to enter waypoint
            if (enterWaypoint != null)
            {
                yield return StartCoroutine(WalkTo(enterWaypoint.position));
            }

            // Walk around museum waypoints (touring the gallery multiple times before heading to exit)
            int currentLoop = 0;
            while (currentLoop < loopsBeforeExit && museumWaypoints != null && museumWaypoints.Length > 0)
            {
                foreach (var wp in museumWaypoints)
                {
                    if (wp != null)
                    {
                        yield return StartCoroutine(WalkTo(wp.position));
                        yield return new WaitForSeconds(waitTimeInside);
                    }
                }
                currentLoop++;
            }
            if (museumWaypoints == null || museumWaypoints.Length == 0)
            {
                // Wait inside fallback
                yield return new WaitForSeconds(waitTimeInside);
            }

            // Walk to exit waypoint
            if (exitWaypoint != null)
            {
                yield return StartCoroutine(WalkTo(exitWaypoint.position));
            }

            // Briefly stop at the exit before looping
            yield return new WaitForSeconds(2f);
        }
    }

    private IEnumerator WalkTo(Vector3 targetPosition)
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            Debug.LogWarning($"[{gameObject.name}] NavMeshAgent is not active or not on NavMesh. Attempting to sample NavMesh position...");
            if (agent != null && NavMesh.SamplePosition(transform.position, out NavMeshHit currentHit, 1.5f, NavMesh.AllAreas))
            {
                agent.Warp(currentHit.position);
            }
            else
            {
                yield break;
            }
        }

        // Ensure target position maps to a valid point on the NavMesh without dropping through floor levels
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit targetHit, 1.5f, NavMesh.AllAreas))
        {
            targetPosition = targetHit.position;
        }

        agent.SetDestination(targetPosition);

        // Wait for path calculation
        while (agent.pathPending)
        {
            yield return null;
        }

        // Check if path is valid or reachable; if not, avoid an infinite walking loop
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning($"[{gameObject.name}] Cannot reach destination: {targetPosition}. Skipping waypoint.");
            yield break;
        }

        float timer = 0f;

        // Wait until destination is reached or timeout occurs (to prevent freezing against walls/obstacles)
        while (agent.remainingDistance > agent.stoppingDistance + 0.1f && timer < maxWalkTimeout)
        {
            timer += Time.deltaTime;

            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                break;
            }

            yield return null;
        }
    }
}
