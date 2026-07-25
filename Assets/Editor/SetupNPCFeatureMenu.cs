using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.Events;

public class SetupNPCFeatureMenu
{
    [MenuItem("Tools/Setup Museum Door and NPC")]
    public static void SetupFeature()
    {
        // 1. Find MissionManager and add Extension
        MissionManager missionManager = Object.FindObjectOfType<MissionManager>();
        if (missionManager == null)
        {
            Debug.LogError("Setup Error: Could not find MissionManager in the scene.");
            return;
        }

        MissionManagerExtension extension = missionManager.gameObject.GetComponent<MissionManagerExtension>();
        if (extension == null)
        {
            extension = missionManager.gameObject.AddComponent<MissionManagerExtension>();
        }

        // 2. Setup Museum Door
        GameObject doorObj = new GameObject("Museum_Door");
        GameObject doorPivot = new GameObject("Pivot");
        doorPivot.transform.SetParent(doorObj.transform);
        GameObject doorMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorMesh.name = "Door_Mesh";
        doorMesh.transform.SetParent(doorPivot.transform);
        doorMesh.transform.localPosition = new Vector3(0.5f, 1f, 0f);
        doorMesh.transform.localScale = new Vector3(1f, 2f, 0.1f);
        
        MuseumDoorController doorController = doorObj.AddComponent<MuseumDoorController>();
        SerializedObject soDoor = new SerializedObject(doorController);
        soDoor.FindProperty("doorPivot").objectReferenceValue = doorPivot.transform;
        soDoor.ApplyModifiedProperties();

        // 3. Setup NPC
        string prefabPath = "Assets/ThirdParties/DenysAlmaral/CityPeople/Prefabs/worker_Male_constructor_B.prefab";
        GameObject npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        GameObject npcInstance = null;
        if (npcPrefab != null)
        {
            npcInstance = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab);
            npcInstance.name = "NPC_Visitor";
        }
        else
        {
            Debug.LogWarning($"Prefab not found at {prefabPath}. Creating a dummy capsule NPC.");
            npcInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcInstance.name = "NPC_Visitor";
        }

        NPCVisitor npcVisitor = npcInstance.AddComponent<NPCVisitor>();
        npcInstance.SetActive(false); // Hide until triggered

        // 4. Create Waypoints
        GameObject wpEnter = new GameObject("Waypoint_EnterMuseum");
        wpEnter.transform.position = new Vector3(0, 0, 5); // Example inside position
        
        GameObject wpExit = new GameObject("Waypoint_ExitMuseum");
        wpExit.transform.position = new Vector3(0, 0, -5); // Example outside position

        npcInstance.transform.position = wpExit.transform.position; // start at exit

        SerializedObject soNpc = new SerializedObject(npcVisitor);
        soNpc.FindProperty("enterWaypoint").objectReferenceValue = wpEnter.transform;
        soNpc.FindProperty("exitWaypoint").objectReferenceValue = wpExit.transform;
        soNpc.ApplyModifiedProperties();

        // 5. Hook up the Events in MissionManagerExtension
        SerializedObject soExtension = new SerializedObject(extension);
        SerializedProperty onMuseumRestoredProp = soExtension.FindProperty("onMuseumRestored");
        
        UnityEventTools.AddPersistentListener(extension.onMuseumRestored, doorController.OpenDoor);
        UnityEventTools.AddPersistentListener(extension.onMuseumRestored, npcVisitor.StartVisiting);
        
        EditorUtility.SetDirty(extension);

        Debug.Log("Museum Door and NPC setup complete! Please position the door and waypoints correctly.");
        Selection.activeGameObject = doorObj;
    }
}
