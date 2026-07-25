using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class SetupHUDMenu
{
    [MenuItem("Tools/Setup HUD Hierarchy")]
    public static void SetupHUD()
    {
        // 1. Setup Main Camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCamera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";
        }

        // 2. Setup Mission object
        GameObject missionObj = new GameObject("Mission");

        // 3. Setup HUD_Canvas
        GameObject canvasObj = new GameObject("HUD_Canvas");
        canvasObj.transform.SetParent(missionObj.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // 4. Setup Timer_Text
        GameObject timerObj = new GameObject("Timer_Text");
        timerObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "00:00";
        timerText.alignment = TextAlignmentOptions.Top;
        timerText.fontSize = 36;
        
        RectTransform timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 1f);
        timerRect.anchorMax = new Vector2(0.5f, 1f);
        timerRect.pivot = new Vector2(0.5f, 1f);
        timerRect.anchoredPosition = new Vector2(0, -30);
        timerRect.sizeDelta = new Vector2(200, 50);

        // 5. Setup Mission 1, 2, 3
        string[] missions = { "Mission_1", "Mission_2", "Mission_3" };
        for (int i = 0; i < missions.Length; i++)
        {
            GameObject mObj = new GameObject(missions[i]);
            mObj.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI mText = mObj.AddComponent<TextMeshProUGUI>();
            mText.text = missions[i].Replace("_", " ");
            mText.alignment = TextAlignmentOptions.TopLeft;
            mText.fontSize = 24;

            RectTransform mRect = mObj.GetComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0f, 1f);
            mRect.anchorMax = new Vector2(0f, 1f);
            mRect.pivot = new Vector2(0f, 1f);
            mRect.anchoredPosition = new Vector2(30, -30 - (i * 40));
            mRect.sizeDelta = new Vector2(200, 40);
        }

        // Add EventSystem if missing
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Selection.activeGameObject = missionObj;
        Debug.Log("HUD Hierarchy setup complete!");
    }
}
