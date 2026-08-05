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

            // Automatically convert display stands, statues, mission tables, ropes, and furniture into carved NavMesh Obstacles so NPCs never walk into or pass through them!
            if (lowerName.Contains("display") || lowerName.Contains("exhibit") || lowerName.Contains("table") || 
                lowerName.Contains("mission") || lowerName.Contains("statue") || lowerName.Contains("sword") || 
                lowerName.Contains("stand") || lowerName.Contains("rope") || lowerName.Contains("bench") || lowerName.Contains("case"))
            {
                UnityEngine.AI.NavMeshObstacle obs = r.gameObject.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                if (obs == null) obs = r.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obs.carving = true;
                obs.carveOnlyStationary = false;
            }
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

        Transform wpDoor1 = GetOrCreateWaypoint("Waypoint_Door1", wpContainer.transform, GetFloorPosition(d1Pos));
        Transform wpDoor2 = GetOrCreateWaypoint("Waypoint_Door2", wpContainer.transform, GetFloorPosition(d2Pos));

        // Create an expansive gallery network around the museum floor (snapped directly onto the walkable NavMesh)
        Transform wpLeftFront  = GetOrCreateWaypoint("Waypoint_Gallery_LeftFront", wpContainer.transform, GetFloorPosition(Vector3.Lerp(d2Pos, d1Pos, 0.2f) - rightDir * 1.8f));
        Transform wpRightFront = GetOrCreateWaypoint("Waypoint_Gallery_RightFront", wpContainer.transform, GetFloorPosition(Vector3.Lerp(d2Pos, d1Pos, 0.25f) + rightDir * 1.8f));
        Transform wpLeftMid    = GetOrCreateWaypoint("Waypoint_Gallery_LeftMid", wpContainer.transform, GetFloorPosition(Vector3.Lerp(d2Pos, d1Pos, 0.5f) - rightDir * 2.2f));
        Transform wpCenter     = GetOrCreateWaypoint("Waypoint_Gallery_Center", wpContainer.transform, GetFloorPosition(Vector3.Lerp(d2Pos, d1Pos, 0.5f) + rightDir * 0.3f));
        Transform wpRightMid   = GetOrCreateWaypoint("Waypoint_Gallery_RightMid", wpContainer.transform, GetFloorPosition(Vector3.Lerp(d2Pos, d1Pos, 0.55f) + rightDir * 2.2f));
        Transform wpLeftBack   = GetOrCreateWaypoint("Waypoint_Gallery_LeftBack", wpContainer.transform, GetFloorPosition(Vector3.Lerp(d2Pos, d1Pos, 0.8f) - rightDir * 1.8f));
        Transform wpRightBack  = GetOrCreateWaypoint("Waypoint_Gallery_RightBack", wpContainer.transform, GetFloorPosition(Vector3.Lerp(d2Pos, d1Pos, 0.75f) + rightDir * 1.8f));
        Transform wpFarEnd     = GetOrCreateWaypoint("Waypoint_Gallery_FarEnd", wpContainer.transform, GetFloorPosition(Vector3.Lerp(d2Pos, d1Pos, 0.88f)));

        string prefabPath1 = "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/worker_Male_constructor_B.prefab";
        string prefabPath2 = "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/city/casual_Female_G.prefab";
        string prefabPath3 = "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/city/casual_Male_G.prefab";
        string prefabPath4 = "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/downtown/casual_Female_K.prefab";

        GameObject npcPrefab1 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath1);
        GameObject npcPrefab2 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath2);
        GameObject npcPrefab3 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath3);
        GameObject npcPrefab4 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath4);
        
        // NPC 1: Complete clockwise tour around the perimeter and back (lift constructor by 0.9f because its model root origin sits at waist level instead of feet)
        NPCVisitor npc1 = CreateNPC(npcPrefab1, "NPC_Visitor_Constructor", GetFloorPosition(d2Pos + new Vector3(-1, 0, -2)), 0.9f);
        SetupNPCWaypoints(npc1, wpDoor2, wpDoor1, new Transform[] { wpLeftFront, wpLeftMid, wpLeftBack, wpFarEnd, wpRightBack, wpRightMid, wpRightFront });

        // NPC 2: Counter-clockwise exploration circuit
        NPCVisitor npc2 = CreateNPC(npcPrefab2, "NPC_Visitor_Casual_Female", GetFloorPosition(d2Pos + new Vector3(1, 0, -3)));
        SetupNPCWaypoints(npc2, wpDoor2, wpDoor1, new Transform[] { wpRightFront, wpRightMid, wpRightBack, wpFarEnd, wpLeftBack, wpLeftMid, wpLeftFront });

        // NPC 3: Crises-crossing center displays and deeper exhibit halls
        NPCVisitor npc3 = CreateNPC(npcPrefab3, "NPC_Visitor_Casual_Male", GetFloorPosition(d2Pos + new Vector3(-2, 0, -4)));
        SetupNPCWaypoints(npc3, wpDoor2, wpDoor1, new Transform[] { wpLeftFront, wpCenter, wpRightBack, wpFarEnd, wpCenter, wpLeftBack });

        // NPC 4: Front and middle exhibition gallery loops
        NPCVisitor npc4 = CreateNPC(npcPrefab4, "NPC_Visitor_Downtown_Female", GetFloorPosition(d2Pos + new Vector3(2, 0, -5)));
        SetupNPCWaypoints(npc4, wpDoor2, wpDoor1, new Transform[] { wpRightFront, wpCenter, wpLeftMid, wpLeftBack, wpRightMid });
        
        EditorUtility.SetDirty(missionManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(missionManager.gameObject.scene);

        Debug.Log("Museum Door and NPCs setup complete! Speed lowered, floor height raycasted, waypoints are now persistent, and NavMesh baked!");
        Selection.activeGameObject = npc1.gameObject;
    }

    private static Transform GetOrCreateWaypoint(string name, Transform parent, Vector3 defaultPos)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            // Update waypoint to fresh NavMesh snapped coordinates when setup is re-run
            child.position = defaultPos;
            return child;
        }
        
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = defaultPos;
        return go.transform;
    }

    private static Vector3 GetFloorPosition(Vector3 startPos)
    {
        float expectedFloorY = startPos.y; // Reference floor height matching door1/door2 elevation

        // Sample NavMesh directly because NavMesh is built directly from visual floor meshes.
        // Avoid Physics.Raycast as decorative floor tiles lack physics colliders and raycasts fall through to basements/terrain below.
        if (UnityEngine.AI.NavMesh.SamplePosition(startPos, out UnityEngine.AI.NavMeshHit navHit, 3f, UnityEngine.AI.NavMesh.AllAreas))
        {
            if (navHit.position.y >= expectedFloorY - 0.3f)
            {
                return navHit.position;
            }
        }

        // Strictly prevent Y coordinate from dipping below floor level
        if (startPos.y < expectedFloorY - 0.3f)
        {
            startPos.y = expectedFloorY;
        }
        return startPos;
    }

    private static NPCVisitor CreateNPC(GameObject prefab, string name, Vector3 startPos, float baseHeightOffset = 0f)
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
        if (visitor != null)
        {
            visitor.baseHeightOffset = baseHeightOffset;
        }
        
        // Assign standard male walking animations to Constructor (default construction tool controller has no walking clips)
        if (name.Contains("Constructor") || (prefab != null && prefab.name.Contains("constructor")))
        {
            RuntimeAnimatorController maleWalkController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/ThirdParties/DenysAlmaral/CityPeople/Animations/City M Animator.controller");
            Animator anim = inst.GetComponent<Animator>();
            if (anim != null && maleWalkController != null)
            {
                anim.runtimeAnimatorController = maleWalkController;
            }
        }

        UnityEngine.AI.NavMeshAgent agent = inst.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = 0.8f;
            agent.stoppingDistance = 0.3f;
            agent.radius = 0.55f; // Keep a generous half-meter buffer distance away from display tables and exhibits
            agent.height = 1.5f;
            agent.baseOffset = baseHeightOffset;
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

    [MenuItem("Tools/Block NPCs From Selected Display (Add NavMesh Obstacle)")]
    public static void BlockSelectedObjectFromNPCs()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("Please select one or more display tables or objects in the Hierarchy first.");
            return;
        }

        foreach (GameObject go in selected)
        {
            UnityEngine.AI.NavMeshObstacle obs = go.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obs == null) obs = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obs.carving = true;
            obs.carveOnlyStationary = false;
            Debug.Log($"[{go.name}] Converted to a carved NavMesh Obstacle. NPCs will no longer walk through or touch this display!");
        }

        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        Debug.Log("NavMesh rebuilt with new display obstacles carved out!");
    }
}
