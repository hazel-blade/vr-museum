using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor.Events;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class InfoCanvasGenerator : EditorWindow
{
    [MenuItem("Tools/VR Museum/Generate InfoCanvases (XR Touch)")]
    public static void ShowWindow()
    {
        GetWindow<InfoCanvasGenerator>("Generate Info Canvases");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto-Generate InfoCanvases for Museum Items", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool copies the Katana_Generic01 InfoCanvas setup for:\n" +
            "• Sword\n" +
            "• Shield\n" +
            "• Anubis\n" +
            "• Pharoah\n\n" +
            "Click the button below to automatically generate all 4 InfoCanvases in your scene.", MessageType.Info);

        if (GUILayout.Button("Generate 4 InfoCanvases", GUILayout.Height(50)))
        {
            GenerateCanvases();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Remove Generated InfoCanvases", GUILayout.Height(30)))
        {
            RemoveGeneratedCanvases();
        }
    }

    private struct TargetData
    {
        public string objectName;
        public string title;
        public string description;

        public TargetData(string objectName, string title, string description)
        {
            this.objectName = objectName;
            this.title = title;
            this.description = description;
        }
    }

    private void GenerateCanvases()
    {
        // 1. Find source Katana InfoCanvas
        GameObject sourceInfoCanvas = FindKatanaInfoCanvas();
        if (sourceInfoCanvas == null)
        {
            Debug.LogError("Could not find 'InfoCanvas' under Katana_Generic01 in the scene!");
            EditorUtility.DisplayDialog("Error", "Could not find Katana_Generic01 InfoCanvas in the scene.", "OK");
            return;
        }

        TargetData[] targets = new TargetData[]
        {
            new TargetData("Sword", "Ancient Egyptian Sword",
                "A bronze Khopesh sword from the New Kingdom era. Used by pharaohs and elites in combat."),

            new TargetData("Shield", "Royal Ceremonial Shield",
                "A wooden shield overlaid with leather and gold leaf hieroglyphs, representing royal strength."),

            new TargetData("Anubis", "Statue of Anubis",
                "A statue depicting Anubis, the ancient Egyptian god of mummification and protector of tombs."),

            new TargetData("Pharoah", "Golden Pharaoh Mask",
                "A golden death mask symbolizing royal authority and passage to the afterlife.")
        };

        int count = 0;

        foreach (var data in targets)
        {
            GameObject targetObj = FindInScene(data.objectName);
            if (targetObj == null)
            {
                Debug.LogWarning($"Target object '{data.objectName}' not found in scene. Skipping.");
                continue;
            }

            // Find or Create _Colliders
            Transform collidersTrans = FindChildRecursiveExact(targetObj.transform, "_Colliders");
            GameObject collidersObj;
            if (collidersTrans == null)
            {
                collidersObj = new GameObject("_Colliders");
                Undo.RegisterCreatedObjectUndo(collidersObj, "Create _Colliders");
                collidersObj.transform.SetParent(targetObj.transform, false);
            }
            else
            {
                collidersObj = collidersTrans.gameObject;
            }

            // Remove existing InfoCanvas under _Colliders if re-generating
            Transform oldCanvas = collidersObj.transform.Find("InfoCanvas");
            if (oldCanvas != null)
            {
                Undo.DestroyObjectImmediate(oldCanvas.gameObject);
            }

            // Ensure Collider on _Colliders
            EnsureCollider(targetObj, collidersObj);

            // Ensure XRSimpleInteractable on _Colliders
            XRSimpleInteractable interactable = collidersObj.GetComponent<XRSimpleInteractable>();
            if (interactable == null)
            {
                interactable = Undo.AddComponent<XRSimpleInteractable>(collidersObj);
            }

            // Instantiate InfoCanvas clone
            bool wasActive = sourceInfoCanvas.activeSelf;
            sourceInfoCanvas.SetActive(true);

            GameObject newCanvas = Instantiate(sourceInfoCanvas, collidersObj.transform);
            Undo.RegisterCreatedObjectUndo(newCanvas, "Create InfoCanvas");
            newCanvas.name = "InfoCanvas";

            sourceInfoCanvas.SetActive(wasActive);

            // Copy transform from source
            newCanvas.transform.localPosition = sourceInfoCanvas.transform.localPosition;
            newCanvas.transform.localRotation = sourceInfoCanvas.transform.localRotation;
            newCanvas.transform.localScale = sourceInfoCanvas.transform.localScale;

            // Update Titles and Texts
            UpdateCanvasTexts(newCanvas, data.title, data.description);

            // Get/Setup UIManager component
            UIManager uiMgr = newCanvas.GetComponent<UIManager>();
            if (uiMgr == null)
            {
                uiMgr = Undo.AddComponent<UIManager>(newCanvas);
            }

            // Assign UIManager fields
            SerializedObject uiMgrSO = new SerializedObject(uiMgr);
            Transform infoTextTrans = FindChildRecursiveExact(newCanvas.transform, "InformationText");
            if (infoTextTrans != null)
            {
                TMP_Text tmpText = infoTextTrans.GetComponent<TMP_Text>();
                if (tmpText != null)
                {
                    uiMgrSO.FindProperty("InformationText").objectReferenceValue = tmpText;
                }
            }
            Canvas canvasComp = newCanvas.GetComponent<Canvas>();
            if (canvasComp != null)
            {
                uiMgrSO.FindProperty("UICanvas").objectReferenceValue = canvasComp;
            }
            uiMgrSO.ApplyModifiedProperties();

            // Wire XRSimpleInteractable events -> UIManager.ToggleUI
            SetupXREvents(interactable, uiMgr);

            // Wire Close button if present
            SetupCloseButton(newCanvas, uiMgr);

            // Set canvas initial active state (false like Katana)
            newCanvas.SetActive(false);

            count++;
            Debug.Log($"✓ Generated InfoCanvas for '{data.objectName}' ({data.title})");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        if (count > 0)
        {
            EditorUtility.DisplayDialog("Success",
                $"Successfully generated {count} InfoCanvas setups!\n\n" +
                "Created for: Sword, Shield, Anubis, Pharoah\n" +
                "When touched/selected in VR, the info canvas will toggle on!\n\n" +
                "Remember to Save Scene (Ctrl+S)!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Notice", "No InfoCanvases were generated. Check Console log.", "OK");
        }
    }

    private void SetupXREvents(XRSimpleInteractable interactable, UIManager uiMgr)
    {
        // Clear existing listeners
        for (int i = interactable.selectEntered.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(interactable.selectEntered, i);

        for (int i = interactable.hoverEntered.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(interactable.hoverEntered, i);

        // Add persistent listener for ToggleUI
        UnityAction toggleAction = System.Delegate.CreateDelegate(typeof(UnityAction), uiMgr, "ToggleUI") as UnityAction;
        if (toggleAction != null)
        {
            UnityEventTools.AddVoidPersistentListener(interactable.selectEntered, toggleAction);
            UnityEventTools.AddVoidPersistentListener(interactable.hoverEntered, toggleAction);
        }
    }

    private void SetupCloseButton(GameObject canvasObj, UIManager uiMgr)
    {
        Transform closeBtnTrans = FindChildRecursiveExact(canvasObj.transform, "Close");
        if (closeBtnTrans == null) closeBtnTrans = FindChildRecursiveExact(canvasObj.transform, "Information");

        if (closeBtnTrans != null)
        {
            Button btn = closeBtnTrans.GetComponent<Button>();
            if (btn != null)
            {
                for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                    UnityEventTools.RemovePersistentListener(btn.onClick, i);

                UnityAction toggleAction = System.Delegate.CreateDelegate(typeof(UnityAction), uiMgr, "ToggleUI") as UnityAction;
                if (toggleAction != null)
                {
                    UnityEventTools.AddVoidPersistentListener(btn.onClick, toggleAction);
                }
            }
        }
    }

    private void EnsureCollider(GameObject targetObj, GameObject collidersObj)
    {
        if (collidersObj.GetComponent<Collider>() != null) return;
        if (collidersObj.GetComponentsInChildren<Collider>().Length > 0) return;

        Renderer[] renderers = targetObj.GetComponentsInChildren<Renderer>(true);
        BoxCollider bc = collidersObj.AddComponent<BoxCollider>();

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int r = 1; r < renderers.Length; r++)
            {
                bounds.Encapsulate(renderers[r].bounds);
            }

            bc.center = collidersObj.transform.InverseTransformPoint(bounds.center);
            Vector3 worldSize = bounds.size;
            Vector3 scale = collidersObj.transform.lossyScale;
            bc.size = new Vector3(
                scale.x != 0 ? Mathf.Abs(worldSize.x / scale.x) : 0.5f,
                scale.y != 0 ? Mathf.Abs(worldSize.y / scale.y) : 0.5f,
                scale.z != 0 ? Mathf.Abs(worldSize.z / scale.z) : 0.5f
            );
        }
        else
        {
            bc.size = new Vector3(0.5f, 0.5f, 0.5f);
        }
    }

    private void UpdateCanvasTexts(GameObject canvasObj, string title, string description)
    {
        TMP_Text[] tmpTexts = canvasObj.GetComponentsInChildren<TMP_Text>(true);
        foreach (var textComp in tmpTexts)
        {
            if (textComp.gameObject.name == "Title" || textComp.gameObject.name.Contains("Title"))
            {
                textComp.text = title;
            }
            else if (textComp.gameObject.name == "InformationText" || textComp.gameObject.name.Contains("Information"))
            {
                textComp.text = description;
            }
        }

        Text[] uiTexts = canvasObj.GetComponentsInChildren<Text>(true);
        foreach (var textComp in uiTexts)
        {
            if (textComp.gameObject.name == "Title" || textComp.gameObject.name.Contains("Title"))
            {
                textComp.text = title;
            }
            else if (textComp.gameObject.name == "InformationText" || textComp.gameObject.name.Contains("Information"))
            {
                textComp.text = description;
            }
        }
    }

    private GameObject FindKatanaInfoCanvas()
    {
        // 1. Search Katana_Generic01 directly
        GameObject katana = FindInScene("Katana_Generic01");
        if (katana != null)
        {
            Transform canvasTrans = FindChildRecursiveExact(katana.transform, "InfoCanvas");
            if (canvasTrans != null) return canvasTrans.gameObject;
        }

        // 2. Search anywhere in loaded scene hierarchy
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform canvasTrans = FindChildRecursiveExact(root.transform, "InfoCanvas");
            if (canvasTrans != null) return canvasTrans.gameObject;
        }

        // 3. Search via Resources / FindObjectsOfTypeAll (finds inactive scene objects too)
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go.name == "InfoCanvas" && go.scene.isLoaded)
            {
                return go;
            }
        }

        return null;
    }

    private void RemoveGeneratedCanvases()
    {
        string[] names = { "Sword", "Shield", "Anubis", "Pharoah" };
        int count = 0;

        foreach (string name in names)
        {
            GameObject target = FindInScene(name);
            if (target == null) continue;

            Transform colliders = FindChildRecursiveExact(target.transform, "_Colliders");
            if (colliders != null)
            {
                Transform canvas = colliders.Find("InfoCanvas");
                if (canvas != null)
                {
                    Undo.DestroyObjectImmediate(canvas.gameObject);
                    count++;
                }
            }

            Transform directCanvas = target.transform.Find("InfoCanvas");
            if (directCanvas != null)
            {
                Undo.DestroyObjectImmediate(directCanvas.gameObject);
                count++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"Removed {count} InfoCanvas object(s).");
    }

    private GameObject FindInScene(string name)
    {
        // Exact match search
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return root;
            Transform found = FindChildRecursiveExact(root.transform, name);
            if (found != null) return found.gameObject;
        }

        // Partial match search (e.g., "Sword_Display" if "Sword" requested)
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0) return root;
            Transform found = FindChildRecursiveContains(root.transform, name);
            if (found != null) return found.gameObject;
        }

        return null;
    }

    private Transform FindChildRecursiveExact(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return child;
            Transform found = FindChildRecursiveExact(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private Transform FindChildRecursiveContains(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0) return child;
            Transform found = FindChildRecursiveContains(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
