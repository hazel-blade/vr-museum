using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject[] npcPrefabs; // Array of prefabs for variety
    public int numNPCsToSpawn = 15;
    
    [Tooltip("Change this value in the Inspector to make NPCs bigger or smaller!")]
    public float npcScale = 0.7f; // Made them even smaller by default!
    
    [Header("Spawn Locations")]
    public Transform door1Spawn;
    public Transform door2Spawn;

    private List<GameObject> spawnedNPCs = new List<GameObject>();

    public void SpawnNPCs()
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0)
        {
            Debug.LogError("NPCSpawner: No NPC prefabs assigned!");
            return;
        }

        if (door1Spawn == null || door2Spawn == null)
        {
            Debug.LogError("NPCSpawner: Door spawn points not assigned!");
            return;
        }

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        for (int i = 0; i < numNPCsToSpawn; i++)
        {
            // Pick a random prefab
            GameObject prefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
            if (prefab == null) continue;

            // Alternate between door 1 and door 2
            Transform spawnPoint = (i % 2 == 0) ? door1Spawn : door2Spawn;
            
            // Add slight random offset to prevent stacking
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
            Vector3 spawnPos = spawnPoint.position + randomOffset;
            
            GameObject newNPC = Instantiate(prefab, spawnPos, spawnPoint.rotation);
            newNPC.name = $"NPC_Visitor_{i}";
            
            // Apply the customizable size!
            newNPC.transform.localScale = new Vector3(npcScale, npcScale, npcScale);
            
            // Ensure they have NavMeshAgent BEFORE NPCVisitor (so Awake finds it)
            NavMeshAgent agent = newNPC.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = newNPC.AddComponent<NavMeshAgent>();
            }
            
            // Make them slim to avoid traffic jams in the doorways!
            agent.radius = 0.25f;
            agent.height = 1.8f;
            agent.speed = 0.8f + Random.Range(-0.2f, 0.2f); // slight speed variation

            NPCVisitor visitor = newNPC.GetComponent<NPCVisitor>();
            if (visitor == null)
            {
                visitor = newNPC.AddComponent<NPCVisitor>();
            }

            // Assign waypoints from the scene
            if (visitor.enterWaypoint == null)
            {
                GameObject wp = GameObject.Find("Museum_Enter_Waypoint");
                if (wp) visitor.enterWaypoint = wp.transform;
            }
            if (visitor.exitWaypoint == null)
            {
                GameObject wp = GameObject.Find("Museum_Exit_Waypoint");
                if (wp) visitor.exitWaypoint = wp.transform;
            }
            if (visitor.museumWaypoints == null || visitor.museumWaypoints.Length == 0)
            {
                GameObject wpContainer = GameObject.Find("Museum_Waypoints");
                if (wpContainer)
                {
                    visitor.museumWaypoints = new Transform[wpContainer.transform.childCount];
                    for (int j = 0; j < wpContainer.transform.childCount; j++)
                    {
                        visitor.museumWaypoints[j] = wpContainer.transform.GetChild(j);
                    }
                }
            }

            visitor.StartVisiting();
            spawnedNPCs.Add(newNPC);

            // Wait 1.5 seconds between each spawn to form a natural line and prevent doorway jams
            yield return new WaitForSeconds(1.5f);
        }

        Debug.Log($"NPCSpawner: Finished spawning {numNPCsToSpawn} NPCs.");
    }
}
