using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;

public class GameLogicUpdater : EditorWindow
{
    [MenuItem("Tools/Update Game Logic Setup")]
    public static void UpdateLogic()
    {
        Debug.Log("Starting Game Logic Update...");

        // 1. Handle NPCs
        NPCVisitor[] existingNPCs = FindObjectsOfType<NPCVisitor>();
        if (existingNPCs.Length > 0)
        {
            foreach (var npc in existingNPCs)
            {
                DestroyImmediate(npc.gameObject);
            }
            Debug.Log($"Removed {existingNPCs.Length} existing NPCs from the scene.");
        }

        // 2. Create NPC Spawner
        NPCSpawner spawner = FindObjectOfType<NPCSpawner>();
        if (spawner == null)
        {
            GameObject spawnerObj = new GameObject("NPCManager");
            spawner = spawnerObj.AddComponent<NPCSpawner>();
            Debug.Log("Created NPCManager and NPCSpawner.");
        }

        // Search for all the great CityPeople prefabs you already have!
        // REPLACED worker_Male_constructor_B with little_boy_B as requested
        string[] searchPaths = new string[]
        {
            "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/city/casual_Female_G.prefab",
            "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/city/casual_Male_G.prefab",
            "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/downtown/casual_Female_K.prefab",
            "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/downtown/casual_Male_K.prefab",
            "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/professions/Doctor_Male_B.prefab",
            "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/professions/police_Female_A.prefab",
            "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/little_kids/little_boy_B.prefab"
        };

        System.Collections.Generic.List<GameObject> loadedPrefabs = new System.Collections.Generic.List<GameObject>();
        foreach(string path in searchPaths)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p != null) loadedPrefabs.Add(p);
        }

        // Fallback just in case
        if (loadedPrefabs.Count == 0)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NPC_Visitor_Prefab.prefab");
            if (p != null) loadedPrefabs.Add(p);
        }

        spawner.npcPrefabs = loadedPrefabs.ToArray();
        spawner.numNPCsToSpawn = 15;

        Transform door1 = GameObject.Find("Door1")?.transform ?? GameObject.Find("Door 1")?.transform;
        if (door1 != null) spawner.door1Spawn = door1;

        Transform door2 = GameObject.Find("Door2")?.transform ?? GameObject.Find("Door 2")?.transform;
        if (door2 != null) spawner.door2Spawn = door2;

        // 3. Setup Stage Interactable Spot on MC_light
        GameObject mcLight = GameObject.Find("MC_light");
        if (mcLight != null)
        {
            // Remove old StageTrigger from MC_light if it exists
            StageTrigger oldTrigger = mcLight.GetComponent<StageTrigger>();
            if (oldTrigger != null) DestroyImmediate(oldTrigger);

            // Look for existing interactable spot and destroy it to recreate cleanly
            Transform oldSpot = mcLight.transform.Find("StageInteractableSpot");
            if (oldSpot != null) DestroyImmediate(oldSpot.gameObject);

            // Create new interactable object
            GameObject spotObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spotObj.name = "StageInteractableSpot";
            spotObj.transform.SetParent(mcLight.transform);
            spotObj.transform.localPosition = new Vector3(0, -1f, 0); 
            spotObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); 
            
            Renderer rend = spotObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = Color.green; 
                rend.sharedMaterial = mat;
            }

            Collider col = spotObj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            spotObj.AddComponent<XRSimpleInteractable>();
            spotObj.AddComponent<StageInteractable>();

            Transform stageWp = mcLight.transform.Find("StageWaypoint");
            if (stageWp == null)
            {
                GameObject wp = new GameObject("StageWaypoint");
                wp.transform.SetParent(mcLight.transform);
                wp.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            Debug.LogWarning("Could not find 'MC_light' in the scene! Please ensure it is named exactly 'MC_light'.");
        }

        Debug.Log("Game Logic Update Complete! Please review the scene and save.");
    }
}
