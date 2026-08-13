using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

/// <summary>
/// Editor tool that generates InfoCanvas + DisplaySpot for each museum display item.
/// 
/// For each item it:
///   1. Clones the Katana InfoCanvas onto the item
///   2. Creates a flat cylinder "spot" on the floor in front of the item
///   3. Attaches DisplaySpotTrigger to the spot, linked to the InfoCanvas
///
/// The spot follows the exact same pattern as StageInteractableSpot in GameLogicUpdater.
/// </summary>
public class InfoCanvasGenerator : EditorWindow
{
    [MenuItem("Tools/VR Museum/Generate InfoCanvases (XR Touch)")]
    public static void ShowWindow()
    {
        GetWindow<InfoCanvasGenerator>("Generate Info Canvases");
    }

    // ─── Data ────────────────────────────────────────────────────────────

    private struct ItemData
    {
        public string objectName;
        public string title;
        public string description;
        public Vector3 spotOffset; // manual offset from item position (world space)

        public ItemData(string objectName, string title, string description, Vector3 spotOffset)
        {
            this.objectName = objectName;
            this.title = title;
            this.description = description;
            this.spotOffset = spotOffset;
        }
    }

    private static readonly ItemData[] items = new ItemData[]
    {
        new ItemData("Sword", "Ancient Egyptian Sword",
            "A bronze Khopesh sword from the New Kingdom era. Used by pharaohs and elites in combat.",
            new Vector3(0, 0, 1.2f)),

        new ItemData("Shield", "Royal Ceremonial Shield",
            "A wooden shield overlaid with leather and gold leaf hieroglyphs, representing royal strength.",
            new Vector3(0, 0, 1.2f)),

        new ItemData("Anubis", "Statue of Anubis",
            "A statue depicting Anubis, the ancient Egyptian god of mummification and protector of tombs.",
            new Vector3(0, 0, 1.2f)),

        new ItemData("Pharoah", "Golden Pharaoh Mask",
            "A golden death mask symbolizing royal authority and passage to the afterlife.",
            new Vector3(0, 0, 1.2f)),
    };

    // ─── GUI ─────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        GUILayout.Label("Auto-Generate InfoCanvases for Museum Items", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool generates for each item:\n" +
            "• An InfoCanvas (cloned from Katana_Generic01)\n" +
            "• A glowing spot on the floor in front of the item\n\n" +
            "When the player stands on the spot, the info panel appears.\n" +
            "When they step off, it disappears.", MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Original 4 Items (Sword, Shield, Anubis, Pharaoh)", GUILayout.Height(30)))
        {
            GenerateAll();
        }

        EditorGUILayout.Space();
        
        GUILayout.Label("Additions", EditorStyles.boldLabel);
        if (GUILayout.Button("Generate Katana Spot Only", GUILayout.Height(40)))
        {
            GenerateKatanaOnly();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Remove All Generated Objects", GUILayout.Height(30)))
        {
            RemoveAll();
        }
    }

    // ─── Katana Generation ───────────────────────────────────────────────
    
    private void GenerateKatanaOnly()
    {
        GameObject sourceCanvas = FindKatanaInfoCanvas();
        if (sourceCanvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find Katana_Generic01 InfoCanvas template.", "OK");
            return;
        }

        GameObject container = GameObject.Find("--- InfoSpots ---");
        if (container == null)
        {
            container = new GameObject("--- InfoSpots ---");
            Undo.RegisterCreatedObjectUndo(container, "Create InfoSpots Container");
        }

        ItemData katanaData = new ItemData("Katana_Generic01", "Japanese Katana",
            "A traditional Japanese sword famously used by samurai. Known for its sharpness, strength, and curved blade.",
            new Vector3(0, 0, 1.2f));

        GameObject targetObj = FindInScene(katanaData.objectName);
        if (targetObj == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find Katana_Generic01 in the scene.", "OK");
            return;
        }

        GameObject infoCanvas = CreateInfoCanvas(sourceCanvas, targetObj, katanaData, container);
        if (infoCanvas != null)
        {
            CreateDisplaySpot(container, targetObj, infoCanvas, katanaData);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                
            EditorUtility.DisplayDialog("Success", "Generated InfoCanvas and Spot for Katana ONLY.\nThe other 4 spots were untouched.", "OK");
        }
    }

    // ─── Main Generation ─────────────────────────────────────────────────

    private void GenerateAll()
    {
        // 1. Find the source InfoCanvas to clone
        GameObject sourceCanvas = FindKatanaInfoCanvas();
        if (sourceCanvas == null)
        {
            Debug.LogError("[InfoCanvasGenerator] Could not find InfoCanvas under Katana_Generic01!");
            EditorUtility.DisplayDialog("Error",
                "Could not find Katana_Generic01 InfoCanvas in the scene.\n" +
                "Make sure it exists in the hierarchy.", "OK");
            return;
        }

        // 2. Find or create a root container for all spots (keeps hierarchy clean)
        GameObject container = GameObject.Find("--- InfoSpots ---");
        if (container == null)
        {
            container = new GameObject("--- InfoSpots ---");
            Undo.RegisterCreatedObjectUndo(container, "Create InfoSpots Container");
        }

        int count = 0;

        foreach (var data in items)
        {
            GameObject targetObj = FindInScene(data.objectName);
            if (targetObj == null)
            {
                Debug.LogWarning($"[InfoCanvasGenerator] '{data.objectName}' not found in scene. Skipping.");
                continue;
            }

            // ── Step A: Create InfoCanvas on the item ──

            GameObject infoCanvas = CreateInfoCanvas(sourceCanvas, targetObj, data, container);
            if (infoCanvas == null) continue;

            // ── Step B: Create the spot on the floor ──

            CreateDisplaySpot(container, targetObj, infoCanvas, data);

            count++;
            Debug.Log($"[InfoCanvasGenerator] ✓ Created InfoCanvas + Spot for '{data.objectName}'");
        }

        // Mark scene dirty so Ctrl+S saves changes
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        if (count > 0)
        {
            EditorUtility.DisplayDialog("Success",
                $"Generated {count} InfoCanvas + Spot setups!\n\n" +
                "Stand on the glowing cyan spot in front of each item to see its info.\n\n" +
                "Remember to Save Scene (Ctrl+S)!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Notice",
                "No objects were generated. Check the Console for warnings.", "OK");
        }
    }

    // ─── InfoCanvas Creation ─────────────────────────────────────────────

    private GameObject CreateInfoCanvas(GameObject source, GameObject target, ItemData data, GameObject container)
    {
        // Find or Create _Colliders child (we still keep the XRSimpleInteractable here for backup interaction on the model itself)
        Transform collidersTrans = target.transform.Find("_Colliders");
        if (collidersTrans == null)
            collidersTrans = FindChildRecursive(target.transform, "_Colliders");

        GameObject collidersObj;
        if (collidersTrans == null)
        {
            collidersObj = new GameObject("_Colliders");
            Undo.RegisterCreatedObjectUndo(collidersObj, "Create _Colliders");
            collidersObj.transform.SetParent(target.transform, false);
        }
        else
        {
            collidersObj = collidersTrans.gameObject;
        }

        // Remove old InfoCanvas from _Colliders if re-generating (from older generator versions)
        Transform oldCanvas = collidersObj.transform.Find("InfoCanvas");
        if (oldCanvas != null && oldCanvas.gameObject != source)
            Undo.DestroyObjectImmediate(oldCanvas.gameObject);

        // Also remove old InfoCanvas from container if re-generating
        Transform oldContainerCanvas = container.transform.Find("InfoCanvas_" + data.objectName);
        if (oldContainerCanvas != null)
            Undo.DestroyObjectImmediate(oldContainerCanvas.gameObject);

        // Ensure XRSimpleInteractable on the item itself
        if (collidersObj.GetComponent<XRSimpleInteractable>() == null)
            Undo.AddComponent<XRSimpleInteractable>(collidersObj);

        // Clone the source canvas
        bool wasActive = source.activeSelf;
        source.SetActive(true);

        // CRITICAL FIX: Parent to the clean container, NOT the model, to avoid extreme FBX scaling distortion
        GameObject newCanvas = Instantiate(source, container.transform);
        Undo.RegisterCreatedObjectUndo(newCanvas, "Create InfoCanvas");
        newCanvas.name = "InfoCanvas_" + data.objectName;
        
        // Clean up any missing scripts that might have been copied from the source or its children
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(newCanvas);
        foreach (Transform t in newCanvas.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }
        source.SetActive(wasActive);

        // Keep original scale
        newCanvas.transform.localScale = source.transform.localScale;
        newCanvas.transform.rotation = source.transform.rotation;

        // Position it just above the item
        newCanvas.transform.position = target.transform.position + new Vector3(0, 1.0f, 0);

        // Update text content
        UpdateTexts(newCanvas, data.title, data.description);

        // Start hidden
        newCanvas.SetActive(false);

        return newCanvas;
    }

    // ─── Display Spot Creation ───────────────────────────────────────────
    // Follows the EXACT same pattern as GameLogicUpdater StageInteractableSpot

    private void CreateDisplaySpot(GameObject container, GameObject targetObj, GameObject infoCanvas, ItemData data)
    {
        string spotName = "InfoSpot_" + data.objectName;

        // Remove old spot
        Transform oldSpot = container.transform.Find(spotName);
        if (oldSpot != null)
            Undo.DestroyObjectImmediate(oldSpot.gameObject);

        // Also clean up old spots that might be parented to the target from previous versions
        Transform oldChildSpot = targetObj.transform.Find("InfoSpot");
        if (oldChildSpot != null)
            Undo.DestroyObjectImmediate(oldChildSpot.gameObject);

        // Create the spot — same approach as GameLogicUpdater line 82-100
        GameObject spotObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(spotObj, "Create InfoSpot");
        spotObj.name = spotName;

        // Parent to container (world-space, no scale interference)
        spotObj.transform.SetParent(container.transform);

        // Position: use the item's world position + the configured offset
        Vector3 itemPos = targetObj.transform.position;
        spotObj.transform.position = new Vector3(
            itemPos.x + data.spotOffset.x,
            0.01f, // just above floor
            itemPos.z + data.spotOffset.z
        );

        // Scale: flat disc
        spotObj.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
        spotObj.transform.rotation = Quaternion.identity;

        // Material: semi-transparent cyan glow
        Renderer rend = spotObj.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            Color cyan = new Color(0.0f, 0.85f, 1.0f, 0.6f);
            mat.color = cyan;

            // Enable transparency on Standard shader
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            // Emission for glow effect
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", cyan * 0.3f);

            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        // Collider: the default CapsuleCollider from CreatePrimitive is fine,
        // but we need it to be a trigger and tall enough to catch the player
        Collider defaultCol = spotObj.GetComponent<Collider>();
        if (defaultCol != null)
            DestroyImmediate(defaultCol);

        // Use a BoxCollider set as trigger. 
        // The cylinder's local Y scale is 0.02, so we need a large local height.
        // World height = localSize.y * localScale.y = 100 * 0.02 = 2.0 meters
        BoxCollider bc = spotObj.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(1.0f, 100f, 1.0f);
        bc.center = new Vector3(0, 50f, 0);

        // CRITICAL: Rigidbody is REQUIRED for OnTriggerEnter/Exit to fire.
        // Without it, Unity silently ignores all trigger collisions.
        Rigidbody rb = spotObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Attach the trigger script and link it to the InfoCanvas
        DisplaySpotTrigger trigger = spotObj.AddComponent<DisplaySpotTrigger>();
        trigger.infoCanvas = infoCanvas;
    }

    // ─── Remove All ──────────────────────────────────────────────────────

    private void RemoveAll()
    {
        int count = 0;

        // Remove spots container
        GameObject container = GameObject.Find("--- InfoSpots ---");
        if (container != null)
        {
            int childCount = container.transform.childCount;
            Undo.DestroyObjectImmediate(container);
            count += childCount;
        }

        // Remove InfoCanvases from items
        foreach (var data in items)
        {
            GameObject target = FindInScene(data.objectName);
            if (target == null) continue;

            // Check under _Colliders
            Transform colliders = target.transform.Find("_Colliders");
            if (colliders == null)
                colliders = FindChildRecursive(target.transform, "_Colliders");

            if (colliders != null)
            {
                Transform canvas = colliders.Find("InfoCanvas");
                // PROTECT the source template from being deleted by the cleanup tool!
                if (canvas != null && data.objectName != "Katana_Generic01")
                {
                    Undo.DestroyObjectImmediate(canvas.gameObject);
                    count++;
                }
            }

            // Check directly on target
            Transform directCanvas = target.transform.Find("InfoCanvas");
            if (directCanvas != null)
            {
                Undo.DestroyObjectImmediate(directCanvas.gameObject);
                count++;
            }

            // Clean up old child spots
            Transform oldSpot = target.transform.Find("InfoSpot");
            if (oldSpot != null)
            {
                Undo.DestroyObjectImmediate(oldSpot.gameObject);
                count++;
            }
        }

        // Also remove the old PlayerProximityTrigger spots container if it exists
        GameObject oldContainer = GameObject.Find("InfoSpotsContainer");
        if (oldContainer != null)
        {
            Undo.DestroyObjectImmediate(oldContainer);
            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[InfoCanvasGenerator] Removed {count} generated object(s).");
        EditorUtility.DisplayDialog("Cleanup", $"Removed {count} generated object(s).", "OK");
    }

    // ─── Text Updater ────────────────────────────────────────────────────

    private void UpdateTexts(GameObject canvasObj, string title, string description)
    {
        // TextMeshPro texts
        TMP_Text[] tmpTexts = canvasObj.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmpTexts)
        {
            if (t.gameObject.name.Contains("Title"))
                t.text = title;
            else if (t.gameObject.name.Contains("Information"))
                t.text = description;
        }

        // Legacy UI texts
        Text[] uiTexts = canvasObj.GetComponentsInChildren<Text>(true);
        foreach (var t in uiTexts)
        {
            if (t.gameObject.name.Contains("Title"))
                t.text = title;
            else if (t.gameObject.name.Contains("Information"))
                t.text = description;
        }
    }

    // ─── Scene Search Helpers ────────────────────────────────────────────

    private GameObject FindKatanaInfoCanvas()
    {
        // 1. Search under Katana_Generic01
        GameObject katana = FindInScene("Katana_Generic01");
        if (katana != null)
        {
            Transform t = FindChildRecursive(katana.transform, "InfoCanvas");
            if (t != null) return t.gameObject;
        }

        // 2. Search all root objects
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform t = FindChildRecursive(root.transform, "InfoCanvas");
            if (t != null) return t.gameObject;
        }

        // 3. FindObjectsOfTypeAll (catches inactive objects)
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == "InfoCanvas" && go.scene.isLoaded)
                return go;
        }

        return null;
    }

    private GameObject FindInScene(string name)
    {
        // Exact match
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return root;
            Transform found = FindChildRecursive(root.transform, name);
            if (found != null) return found.gameObject;
        }

        // Partial match (e.g. "Sword_Display" when searching "Sword")
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return root;
            Transform found = FindChildContains(root.transform, name);
            if (found != null) return found.gameObject;
        }

        return null;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private Transform FindChildContains(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return child;
            Transform found = FindChildContains(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
