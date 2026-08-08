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
    
    private bool isHeadingToStage = false;

    private void Awake()
    {
        initialFloorY = transform.position.y;
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.5f;
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

        // First, check if the Animator Controller explicitly defines a boolean parameter
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

            // Scan clip names to find pure walk and idle
            if (!hasWalkingParam && animator.runtimeAnimatorController.animationClips != null)
            {
                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                {
                    string lowerName = clip.name.ToLower();
                    // Prefer basic walk/idle over complex ones
                    if (string.IsNullOrEmpty(walkStateName) && (lowerName.Contains("walk") || lowerName.Contains("jog")))
                    {
                        walkStateName = clip.name;
                    }
                    else if (string.IsNullOrEmpty(idleStateName) && lowerName.Contains("idle") && !lowerName.Contains("phone") && !lowerName.Contains("sit") && !lowerName.Contains("talk"))
                    {
                        idleStateName = clip.name;
                    }
                }
            }

            if (string.IsNullOrEmpty(walkStateName)) walkStateName = "locom_m_basicWalk_30f";
            if (string.IsNullOrEmpty(idleStateName)) idleStateName = "idle_m_2_220f";
        }

        animationInitialized = true;
    }

    private void Update()
    {
        if (animator == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        if (agent.baseOffset != baseHeightOffset)
        {
            agent.baseOffset = baseHeightOffset;
        }

        // Anti-clipping safety guard
        if (transform.position.y < initialFloorY - 0.4f)
        {
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

        bool isMoving = agent.velocity.sqrMagnitude > 0.005f && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance);

        if (isMoving != isCurrentlyWalking)
        {
            isCurrentlyWalking = isMoving;
            ApplyAnimationState(isMoving);
        }
        else
        {
            // AGGRESSIVELY ENFORCE walk or idle state so they don't randomly play other animations!
            if (!hasWalkingParam)
            {
                string targetState = isCurrentlyWalking ? walkStateName : idleStateName;
                if (!string.IsNullOrEmpty(targetState))
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    // If the animator drifted into a different animation (like sitting or checking phone), force it back!
                    if (!stateInfo.IsName(targetState) && !animator.IsInTransition(0))
                    {
                        animator.CrossFadeInFixedTime(targetState, 0.1f);
                    }
                }
            }
        }
        
        // If at stage, rotate to face it
        if (isHeadingToStage && !isMoving && agent.remainingDistance <= agent.stoppingDistance)
        {
            GameObject stage = GameObject.Find("ModularStage") ?? GameObject.Find("MC_light");
            if (stage != null)
            {
                Vector3 dir = (stage.transform.position - transform.position).normalized;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
                }
            }
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
                animator.CrossFadeInFixedTime(targetState, 0.25f);
            }
        }
    }

    public void StartVisiting()
    {
        gameObject.SetActive(true);
        isHeadingToStage = false;
        StopAllCoroutines();
        StartCoroutine(VisitRoutine());
    }
    
    public void GoToStage()
    {
        isHeadingToStage = true;
        StopAllCoroutines();
        StartCoroutine(GoToStageRoutine());
    }
    
    private IEnumerator GoToStageRoutine()
    {
        GameObject stageWp = GameObject.Find("StageWaypoint");
        Vector3 targetPos;
        
        if (stageWp != null)
        {
            Vector2 rand = Random.insideUnitCircle * 3f;
            targetPos = stageWp.transform.position + new Vector3(rand.x, 0, rand.y);
        }
        else
        {
            GameObject stage = GameObject.Find("ModularStage") ?? GameObject.Find("MC_light");
            if (stage != null)
            {
                Vector2 rand = Random.insideUnitCircle * 3f;
                targetPos = stage.transform.position + new Vector3(rand.x, 0, rand.y - 3f);
            }
            else
            {
                yield break;
            }
        }

        yield return StartCoroutine(WalkTo(targetPos));
    }

    private IEnumerator VisitRoutine()
    {
        Vector3 startPos = transform.position; 
        yield return null;

        while (true)
        {
            if (NavMesh.SamplePosition(startPos, out NavMeshHit startHit, 1.5f, NavMesh.AllAreas))
            {
                startPos = startHit.position;
            }

            if (agent != null && agent.isActiveAndEnabled) agent.Warp(startPos);
            else transform.position = startPos;

            yield return null;

            if (enterWaypoint != null) yield return StartCoroutine(WalkTo(enterWaypoint.position));

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
            if (museumWaypoints == null || museumWaypoints.Length == 0) yield return new WaitForSeconds(waitTimeInside);

            if (exitWaypoint != null) yield return StartCoroutine(WalkTo(exitWaypoint.position));

            yield return new WaitForSeconds(2f);
        }
    }

    private IEnumerator WalkTo(Vector3 targetPosition)
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            if (agent != null && NavMesh.SamplePosition(transform.position, out NavMeshHit currentHit, 1.5f, NavMesh.AllAreas))
            {
                agent.Warp(currentHit.position);
            }
            else yield break;
        }

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit targetHit, 1.5f, NavMesh.AllAreas))
        {
            targetPosition = targetHit.position;
        }

        agent.SetDestination(targetPosition);
        while (agent.pathPending) yield return null;
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) yield break;

        float timer = 0f;
        while (agent.remainingDistance > agent.stoppingDistance + 0.1f && timer < maxWalkTimeout)
        {
            timer += Time.deltaTime;
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid) break;
            yield return null;
        }
    }
}
