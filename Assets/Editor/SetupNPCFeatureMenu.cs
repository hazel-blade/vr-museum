using UnityEngine;
using UnityEditor;

public class SetupNPCFeatureMenu
{
    [MenuItem("Tools/Setup Museum Door and NPC")]
    public static void SetupFeature()
    {
        MissionManager missionManager = Object.FindObjectOfType<MissionManager>();
        if (missionManager == null)
        {
            Debug.LogError("Setup Error: Could not find MissionManager in the scene.");
            return;
        }

        var oldExt = missionManager.gameObject.GetComponent("MissionManagerExtension");
        if (oldExt != null) Object.DestroyImmediate(oldExt);

        GameObject door1 = GameObject.Find("door1");
        GameObject door2 = GameObject.Find("door2");
        
        SetupDoubleDoors(door1);
        SetupDoubleDoors(door2);

        if (door1 == null && door2 == null)
        {
            Debug.LogWarning("Could not find 'door1' or 'door2' in the scene.");
        }

        // Bake NavMesh to prevent them from passing through objects!
        Debug.Log("Auto-baking NavMesh so NPCs avoid objects...");
        MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();
        foreach (var r in renderers)
        {
            string lowerName = r.gameObject.name.ToLower();
            if (lowerName.Contains("door") || lowerName.Contains("npc") || lowerName.Contains("player") || lowerName.Contains("waypoint"))
                continue;
            
            var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags | StaticEditorFlags.NavigationStatic);
        }
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

        // Clear only old NPCs, KEEP waypoints so user's manual changes aren't lost!
        NPCVisitor[] oldNpcs = Object.FindObjectsOfType<NPCVisitor>(true);
        foreach(var n in oldNpcs) Object.DestroyImmediate(n.gameObject);

        GameObject wpContainer = GameObject.Find("Museum_Waypoints");
        if (wpContainer == null)
        {
            wpContainer = new GameObject("Museum_Waypoints");
        }

        Vector3 d1Pos = door1 != null ? door1.transform.position : new Vector3(0, 0, 5);
        Vector3 d2Pos = door2 != null ? door2.transform.position : new Vector3(0, 0, -5);
        Vector3 pathDir = (d1Pos - d2Pos).normalized;
        Vector3 rightDir = Vector3.Cross(Vector3.up, pathDir).normalized;

        Transform wpDoor1 = GetOrCreateWaypoint("Waypoint_Door1", wpContainer.transform, d1Pos);
        Transform wpDoor2 = GetOrCreateWaypoint("Waypoint_Door2", wpContainer.transform, d2Pos);
        Transform wpInside1 = GetOrCreateWaypoint("Waypoint_Inside1", wpContainer.transform, Vector3.Lerp(d2Pos, d1Pos, 0.3f) + rightDir * 1.5f);
        Transform wpInside2 = GetOrCreateWaypoint("Waypoint_Inside2", wpContainer.transform, Vector3.Lerp(d2Pos, d1Pos, 0.7f) - rightDir * 1.5f);
        Transform wpInside3 = GetOrCreateWaypoint("Waypoint_Inside3", wpContainer.transform, Vector3.Lerp(d2Pos, d1Pos, 0.5f) + rightDir * 0.8f);
        Transform wpInside4 = GetOrCreateWaypoint("Waypoint_Inside4", wpContainer.transform, Vector3.Lerp(d2Pos, d1Pos, 0.4f) - rightDir * 0.8f);

        string prefabPath1 = "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/worker_Male_constructor_B.prefab";
        string prefabPath2 = "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/city/casual_Female_G.prefab";
        string prefabPath3 = "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/city/casual_Male_G.prefab";
        string prefabPath4 = "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/downtown/casual_Female_K.prefab";

        GameObject npcPrefab1 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath1);
        GameObject npcPrefab2 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath2);
        GameObject npcPrefab3 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath3);
        GameObject npcPrefab4 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath4);
        
        NPCVisitor npc1 = CreateNPC(npcPrefab1, "NPC_Visitor_Constructor", GetFloorPosition(d2Pos + new Vector3(-1, 0, -2)));
        SetupNPCWaypoints(npc1, wpDoor2, wpDoor1, new Transform[] { wpInside1, wpInside2 });

        NPCVisitor npc2 = CreateNPC(npcPrefab2, "NPC_Visitor_Casual_Female", GetFloorPosition(d2Pos + new Vector3(1, 0, -3)));
        SetupNPCWaypoints(npc2, wpDoor2, wpDoor1, new Transform[] { wpInside3, wpInside4, wpInside1 });

        NPCVisitor npc3 = CreateNPC(npcPrefab3, "NPC_Visitor_Casual_Male", GetFloorPosition(d2Pos + new Vector3(-2, 0, -4)));
        SetupNPCWaypoints(npc3, wpDoor2, wpDoor1, new Transform[] { wpInside2, wpInside3 });

        NPCVisitor npc4 = CreateNPC(npcPrefab4, "NPC_Visitor_Downtown_Female", GetFloorPosition(d2Pos + new Vector3(2, 0, -5)));
        SetupNPCWaypoints(npc4, wpDoor2, wpDoor1, new Transform[] { wpInside4, wpInside1, wpInside2 });
        
        EditorUtility.SetDirty(missionManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(missionManager.gameObject.scene);

        Debug.Log("Museum Door and NPCs setup complete! Speed lowered, floor height raycasted, waypoints are now persistent, and NavMesh baked!");
        Selection.activeGameObject = npc1.gameObject;
    }

    private static Transform GetOrCreateWaypoint(string name, Transform parent, Vector3 defaultPos)
    {
        Transform child = parent.Find(name);
        if (child != null) return child;
        
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = defaultPos;
        return go.transform;
    }

    private static Vector3 GetFloorPosition(Vector3 startPos)
    {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(startPos.x, 10f, startPos.z), Vector3.down, out hit, 20f))
        {
            return hit.point;
        }
        return startPos;
    }

    private static NPCVisitor CreateNPC(GameObject prefab, string name, Vector3 startPos)
    {
        GameObject inst = null;
        if (prefab != null)
        {
            inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = name;
        }
        else
        {
            inst = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            inst.name = name;
            startPos.y += 1f; // Capsule pivot is in center, so raise by 1
        }

        NPCVisitor visitor = inst.AddComponent<NPCVisitor>();
        
        UnityEngine.AI.NavMeshAgent agent = inst.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = 0.8f;
            agent.radius = 0.3f; // Make them skinny to fit through tight exhibits
            agent.height = 1.5f;
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }

        inst.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
        inst.transform.position = startPos;
        inst.SetActive(false);
        return visitor;
    }

    private static void SetupNPCWaypoints(NPCVisitor visitor, Transform enter, Transform exit, Transform[] inside)
    {
        SerializedObject soNpc = new SerializedObject(visitor);
        soNpc.FindProperty("enterWaypoint").objectReferenceValue = enter;
        soNpc.FindProperty("exitWaypoint").objectReferenceValue = exit;
        soNpc.FindProperty("moveSpeed").floatValue = 0.8f; // Slow down speed
        
        SerializedProperty wpArray = soNpc.FindProperty("museumWaypoints");
        if (wpArray != null)
        {
            wpArray.arraySize = inside.Length;
            for (int i = 0; i < inside.Length; i++)
            {
                wpArray.GetArrayElementAtIndex(i).objectReferenceValue = inside[i];
            }
        }
        soNpc.ApplyModifiedProperties();
    }

    private static void SetupDoubleDoors(GameObject parentDoor)
    {
        if (parentDoor == null) return;

        MuseumDoorController[] existingCtrls = parentDoor.GetComponentsInChildren<MuseumDoorController>(true);
        foreach (var ctrl in existingCtrls)
        {
            Object.DestroyImmediate(ctrl);
        }

        Transform doorL = null;
        Transform doorR = null;

        foreach (Transform child in parentDoor.transform)
        {
            string lowerName = child.name.ToLower();
            if (lowerName.Contains("l")) doorL = child;
            else if (lowerName.Contains("r")) doorR = child;
        }

        if (doorL != null)
        {
            MuseumDoorController ctrlL = doorL.gameObject.AddComponent<MuseumDoorController>();
            SerializedObject soL = new SerializedObject(ctrlL);
            soL.FindProperty("doorPivot").objectReferenceValue = doorL;
            soL.FindProperty("openAngle").floatValue = 90f;
            soL.ApplyModifiedProperties();
        }

        if (doorR != null)
        {
            MuseumDoorController ctrlR = doorR.gameObject.AddComponent<MuseumDoorController>();
            SerializedObject soR = new SerializedObject(ctrlR);
            soR.FindProperty("doorPivot").objectReferenceValue = doorR;
            soR.FindProperty("openAngle").floatValue = -90f;
            soR.ApplyModifiedProperties();
        }
    }
}
