using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class MissionManager : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onMuseumRestored;

    public static MissionManager Instance { get; private set; }

    [Header("Mission Settings")]
    [SerializeField] private float timeLimitSeconds = 600f; // 10 minutes
    [SerializeField] private string sceneToRestart = "SampleScene";

    [Header("Ending Panels")]
    [SerializeField] private GameObject timeOutPanel; // Shows when time runs out
    [SerializeField] private GameObject victoryPanel; // Shows when you win (optional)
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Blackout Settings")]
    [SerializeField] private Light mainDirectionalLight;
    [SerializeField] private Color blackoutAmbientColor = Color.black;
    [SerializeField] private Color normalAmbientColor = Color.white;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [Header("Mission UI Checks")]
    [SerializeField] private GameObject generatorCheckmark; // Mission 1
    [SerializeField] private GameObject nodachiCheckmark; // Mission 2 (Replace Both Items)
    [SerializeField] private GameObject sayaNodachiCheckmark; // Mission 3 (Go to Stage)

    private float remainingTime;
    private bool isTimerRunning = false;

    // Mission states
    private bool isGeneratorRunning = false;
    private bool isItem1Replaced = false;
    private bool isItem2Replaced = false;
    private bool isMission2Complete = false;
    public bool isMuseumOpen = false;
    private bool isStageReached = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartBlackoutMission();
    }

    private void Update()
    {
        // Timer runs until the player reaches the stage (Mission 3 ends)
        if (isTimerRunning && !isStageReached)
        {
            remainingTime -= Time.deltaTime;

            if (timerText != null)
            {
                UpdateTimerUI();
            }

            if (remainingTime <= 0f)
            {
                OnTimeRanOut();
            }
        }
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        
        if (isMuseumOpen)
        {
            // Mission 2 is complete, so show the Event Started text but keep the timer ticking for Mission 3!
            timerText.text = string.Format("Event Started!\nTime left: {0:00}:{1:00}", minutes, seconds);
        }
        else
        {
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        if (remainingTime <= 60f)
        {
            timerText.color = Color.red;
        }
    }

    private void StartBlackoutMission()
    {
        remainingTime = timeLimitSeconds;
        isTimerRunning = true;
        isMuseumOpen = false;
        isStageReached = false;
        isMission2Complete = false;
        isItem1Replaced = false;
        isItem2Replaced = false;

        if (mainDirectionalLight != null)
            mainDirectionalLight.enabled = false;
        
        RenderSettings.ambientLight = blackoutAmbientColor;

        if (generatorCheckmark != null) generatorCheckmark.SetActive(false);
        if (nodachiCheckmark != null) nodachiCheckmark.SetActive(false);
        if (sayaNodachiCheckmark != null) sayaNodachiCheckmark.SetActive(false);

        Debug.Log("Blackout started! You have 10 minutes to restore the museum and reach the stage.");
    }

    public void CompleteGenerator()
    {
        if (isGeneratorRunning) return;
        isGeneratorRunning = true;
        
        if (generatorCheckmark != null) generatorCheckmark.SetActive(true);

        Debug.Log("Mission Update: Generator is running (Mission 1 Complete).");
        CheckMuseumOpen();
    }

    public void CompleteItem1()
    {
        if (isItem1Replaced) return;
        isItem1Replaced = true;
        Debug.Log("Mission Update: Item 1 replaced.");
        CheckMission2Complete();
    }

    public void CompleteItem2()
    {
        if (isItem2Replaced) return;
        isItem2Replaced = true;
        Debug.Log("Mission Update: Item 2 replaced.");
        CheckMission2Complete();
    }

    private void CheckMission2Complete()
    {
        if (isItem1Replaced && isItem2Replaced && !isMission2Complete)
        {
            isMission2Complete = true;
            if (nodachiCheckmark != null) nodachiCheckmark.SetActive(true);
            Debug.Log("Mission Update: Both items replaced (Mission 2 Complete).");
            CheckMuseumOpen();
        }
    }

    private void CheckMuseumOpen()
    {
        // Museum opens when Mission 1 and Mission 2 are complete
        if (isGeneratorRunning && isMission2Complete && !isMuseumOpen)
        {
            OpenMuseum();
        }
    }

    private void OpenMuseum()
    {
        isMuseumOpen = true;

        if (timerText != null)
        {
            timerText.color = Color.yellow;
            // Immediate UI update so the user sees "Event Started!" the exact frame Mission 2 completes
            UpdateTimerUI();
        }

        if (mainDirectionalLight != null)
            mainDirectionalLight.enabled = true;
        
        RenderSettings.ambientLight = normalAmbientColor;

        Debug.Log("Museum restored! Now get to the Stage before time runs out! (Mission 3)");
        
        MuseumDoorController[] doors = FindObjectsOfType<MuseumDoorController>();
        foreach (var door in doors)
        {
            if (door != null) door.OpenDoor();
        }

        // Start spawning NPCs or activate them
        NPCSpawner spawner = FindObjectOfType<NPCSpawner>();
        if (spawner != null)
        {
            spawner.SpawnNPCs();
        }
        else
        {
            NPCVisitor[] npcs = FindObjectsOfType<NPCVisitor>(true);
            foreach (var npc in npcs)
            {
                if (npc != null) npc.StartVisiting();
            }
        }

        onMuseumRestored?.Invoke();
    }

    public void OnStageReached()
    {
        // Only valid if museum is open (Mission 1 and 2 complete)
        if (!isMuseumOpen || isStageReached) return;

        isStageReached = true;
        isTimerRunning = false; // Stop the timer

        if (sayaNodachiCheckmark != null) sayaNodachiCheckmark.SetActive(true); // Mission 3 Complete UI

        if (timerText != null)
        {
            timerText.color = Color.green;
            timerText.text = "Goal Reached!";
        }

        Debug.Log("Stage reached! Mission 3 Complete. NPCs are gathering.");

        // Tell all NPCs to go to the stage
        NPCVisitor[] npcs = FindObjectsOfType<NPCVisitor>();
        foreach (var npc in npcs)
        {
            if (npc != null) npc.GoToStage();
        }

        StartCoroutine(EndGameRoutine());
    }

    private IEnumerator EndGameRoutine()
    {
        // Wait 15 seconds for NPCs to gather and the player to enjoy the scene
        yield return new WaitForSeconds(15f);
        
        Debug.Log("Game Won! Going to Ending Scene.");
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("EndingScene");
    }

    private void CreateVictoryText()
    {
        GameObject canvasObj = new GameObject("VictoryCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform rt = canvasObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 300);
        
        // Scale it down significantly so it fits in VR
        canvasObj.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
        
        // Position it 2 meters in front of the main camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            canvasObj.transform.position = mainCam.transform.position + mainCam.transform.forward * 2f;
            canvasObj.transform.rotation = Quaternion.LookRotation(canvasObj.transform.position - mainCam.transform.position);
        }
        else
        {
            canvasObj.transform.position = new Vector3(0, 2f, 0);
        }

        GameObject textObj = new GameObject("VictoryText");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "VICTORY!\nEvent Completed";
        text.fontSize = 100;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.yellow;
        text.fontStyle = FontStyles.Bold;
        
        // Ensure text is centered on canvas
        RectTransform textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = Vector2.zero;
    }

    private void OnTimeRanOut()
    {
        isTimerRunning = false;
        Debug.Log("Time ran out! Showing options...");
        if (timeOutPanel != null)
        {
            timeOutPanel.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene(sceneToRestart);
        }
    }

    public void ChooseCheckpoint()
    {
        if (timeOutPanel != null)
            timeOutPanel.SetActive(false);

        int completedCount = 0;
        if (isGeneratorRunning) completedCount++;
        if (isMission2Complete) completedCount++;

        if (completedCount >= 1)
        {
            remainingTime = timeLimitSeconds / 2f; // Get half time
            Debug.Log($"Restarting from checkpoint. Time left: {remainingTime}s");
            isTimerRunning = true;
            isStageReached = false;
            if (timerText != null)
            {
                timerText.color = Color.white;
                UpdateTimerUI();
            }
        }
        else
        {
            SceneManager.LoadScene(sceneToRestart);
        }
    }

    public void ChooseMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    [ContextMenu("DEBUG: 1. Complete Mission 1 (Generator)")]
    public void DebugCompleteMission1()
    {
        Debug.Log("DEBUG: Forcing Mission 1 Complete.");
        CompleteGenerator();
    }

    [ContextMenu("DEBUG: 2. Complete Mission 2 (Sockets)")]
    public void DebugCompleteMission2()
    {
        Debug.Log("DEBUG: Forcing Mission 2 Complete.");
        CompleteItem1();
        CompleteItem2();
    }

    [ContextMenu("DEBUG: 3. Complete Mission 3 (Stage)")]
    public void DebugCompleteMission3()
    {
        Debug.Log("DEBUG: Forcing Mission 3 Complete.");
        OnStageReached();
    }

    [ContextMenu("DEBUG: Complete ALL Missions")]
    public void DebugCompleteAllMissions()
    {
        Debug.Log("DEBUG: Auto-completing all missions...");
        CompleteGenerator();
        CompleteItem1();
        CompleteItem2();
        Invoke("OnStageReached", 2f);
    }
}
