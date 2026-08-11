using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor.Events;

public class EndingSceneSetupWindow : EditorWindow
{
    [MenuItem("Tools/VR Museum/Ending Scene Auto Setup")]
    public static void ShowWindow()
    {
        GetWindow<EndingSceneSetupWindow>("Ending Scene Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto Setup Ending Scene UI", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("This tool will create a UI Canvas, add the 'Thanks for playing' text, the Main Menu button, and automatically configure the EndingSceneController for you.", MessageType.Info);

        if (GUILayout.Button("Run Auto Setup", GUILayout.Height(40)))
        {
            SetupEndingScene();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Fix Main Menu Button Link", GUILayout.Height(40)))
        {
            FixButton();
        }
    }

    private void FixButton()
    {
        EndingSceneController controller = FindObjectOfType<EndingSceneController>();
        if (controller == null)
        {
            Debug.LogError("Could not find EndingSceneManager with EndingSceneController script.");
            return;
        }

        Button button = null;
        if (controller.mainMenuButton != null)
        {
            button = controller.mainMenuButton.GetComponent<Button>();
        }
        else
        {
            GameObject btnObj = GameObject.Find("Main Menu Button");
            if (btnObj == null) btnObj = GameObject.Find("MainMenuButton");
            if (btnObj != null) button = btnObj.GetComponent<Button>();
        }

        if (button == null)
        {
            Debug.LogError("Could not find the Button component to fix.");
            return;
        }

        // Clear broken listeners
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        }

        // Add correct listener
        UnityAction action = System.Delegate.CreateDelegate(typeof(UnityAction), controller, "OnMainMenuButtonClicked") as UnityAction;
        UnityEventTools.AddVoidPersistentListener(button.onClick, action);
        
        // Force Unity to save the change
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        
        Selection.activeGameObject = button.gameObject;
        Debug.Log("Main Menu Button link successfully fixed!");
    }

    private void SetupEndingScene()
    {
        // 1. Find or create a Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 2. Find or Create EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 3. Create Manager Object
        GameObject managerObj = new GameObject("EndingSceneManager");
        EndingSceneController controller = managerObj.AddComponent<EndingSceneController>();

        // 4. Create Thanks Text
        GameObject textObj = new GameObject("ThanksText");
        textObj.transform.SetParent(canvas.transform, false);
        Text thanksText = textObj.AddComponent<Text>();
        thanksText.text = "Thanks for playing!";
        thanksText.fontSize = 48;
        thanksText.alignment = TextAnchor.MiddleCenter;
        
        // Use Arial font as default
        thanksText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0, 50);
        textRect.sizeDelta = new Vector2(600, 100);

        // 5. Create Button
        GameObject buttonObj = new GameObject("MainMenuButton");
        buttonObj.transform.SetParent(canvas.transform, false);
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        button.targetGraphic = buttonImage;
        
        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        Text buttonText = buttonTextObj.AddComponent<Text>();
        buttonText.text = "Back to Main Menu";
        buttonText.fontSize = 24;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.color = Color.black;

        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(0, -50);
        buttonRect.sizeDelta = new Vector2(250, 60);

        // 6. Connect everything
        controller.thanksForPlayingText = textObj;
        controller.mainMenuButton = buttonObj;

        // 7. Setup Button OnClick
        UnityAction methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction), controller, "OnMainMenuButtonClicked") as UnityAction;
        UnityEventTools.AddPersistentListener(button.onClick, methodDelegate);

        // Select the manager
        Selection.activeGameObject = managerObj;

        Debug.Log("Ending Scene UI Auto Setup Complete!");
    }
}
