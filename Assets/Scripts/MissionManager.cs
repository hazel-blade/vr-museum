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

    [Header("Time Out Settings")]
    [SerializeField] private GameObject timeOutPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Blackout Settings")]
    [SerializeField] private Light mainDirectionalLight;
    [SerializeField] private Color blackoutAmbientColor = Color.black;
    [SerializeField] private Color normalAmbientColor = Color.white;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [Header("Mission UI Checks")]
    [SerializeField] private GameObject generatorCheckmark;
    [SerializeField] private GameObject nodachiCheckmark;
    [SerializeField] private GameObject sayaNodachiCheckmark;

    private float remainingTime;
    private bool isTimerRunning = false;

    // Mission states
    private bool isGeneratorRunning = false;
    private bool isNodachiReplaced = false;
    private bool isSayaNodachiReplaced = false;
    public bool isMuseumOpen = false;

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
        if (isTimerRunning && !isMuseumOpen)
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
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        // Optional: Make text red when less than 1 minute remains
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

        // Apply blackout visual effect
        if (mainDirectionalLight != null)
            mainDirectionalLight.enabled = false;
        
        RenderSettings.ambientLight = blackoutAmbientColor;

        // Hide checkmarks at start
        if (generatorCheckmark != null) generatorCheckmark.SetActive(false);
        if (nodachiCheckmark != null) nodachiCheckmark.SetActive(false);
        if (sayaNodachiCheckmark != null) sayaNodachiCheckmark.SetActive(false);

        Debug.Log("Blackout started! You have 10 minutes to restore the museum.");
    }

    public void CompleteGenerator()
    {
        if (isGeneratorRunning) return;
        isGeneratorRunning = true;
        
        if (generatorCheckmark != null) generatorCheckmark.SetActive(true);

        Debug.Log("Mission Update: Generator is running.");
        CheckMissionComplete();
    }

    public void CompleteNodachi()
    {
        if (isNodachiReplaced) return;
        isNodachiReplaced = true;
        
        if (nodachiCheckmark != null) nodachiCheckmark.SetActive(true);

        Debug.Log("Mission Update: Nodachi replaced.");
        CheckMissionComplete();
    }

    public void CompleteSayaNodachi()
    {
        if (isSayaNodachiReplaced) return;
        isSayaNodachiReplaced = true;
        
        if (sayaNodachiCheckmark != null) sayaNodachiCheckmark.SetActive(true);

        Debug.Log("Mission Update: Saya_Nodachi replaced.");
        CheckMissionComplete();
    }

    // Keep backwards compatibility for any existing calls
    public void CompleteItem1() => CompleteNodachi();
    public void CompleteItem2() => CompleteSayaNodachi();

    private void CheckMissionComplete()
    {
        if (isGeneratorRunning && isNodachiReplaced && isSayaNodachiReplaced)
        {
            OnMissionSuccess();
        }
    }

    private void OnMissionSuccess()
    {
        isTimerRunning = false;
        isMuseumOpen = true;

        if (timerText != null)
        {
            timerText.color = Color.green;
            timerText.text = "Museum Restored!";
        }

        // Restore visuals
        if (mainDirectionalLight != null)
            mainDirectionalLight.enabled = true;
        
        RenderSettings.ambientLight = normalAmbientColor;

        Debug.Log("All missions completed! The museum is open.");
        
        // Find and open all doors in the scene automatically
        MuseumDoorController[] doors = FindObjectsOfType<MuseumDoorController>();
        foreach (var door in doors)
        {
            if (door != null) door.OpenDoor();
        }

        // Find and trigger all NPCs in the scene (including hidden ones)
        NPCVisitor[] npcs = FindObjectsOfType<NPCVisitor>(true);
        foreach (var npc in npcs)
        {
            if (npc != null) npc.StartVisiting();
        }

        onMuseumRestored?.Invoke();
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
            // Fallback if no UI is set
            SceneManager.LoadScene(sceneToRestart);
        }
    }

    public void ChooseCheckpoint()
    {
        if (timeOutPanel != null)
            timeOutPanel.SetActive(false);

        int completedCount = 0;
        if (isGeneratorRunning) completedCount++;
        if (isNodachiReplaced) completedCount++;
        if (isSayaNodachiReplaced) completedCount++;

        if (completedCount >= 1)
        {
            remainingTime = timeLimitSeconds / 2f; // Get half time (e.g., 5 mins)
            Debug.Log($"Restarting from checkpoint. Keep completed missions. Time left: {remainingTime}s");
            isTimerRunning = true;
            if (timerText != null)
            {
                timerText.color = Color.white;
                UpdateTimerUI();
            }
        }
        else
        {
            // If no missions are completed, restart from the beginning
            SceneManager.LoadScene(sceneToRestart);
        }
    }

    public void ChooseMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    [ContextMenu("Debug Complete All Missions")]
    public void DebugCompleteAllMissions()
    {
        Debug.Log("DEBUG: Auto-completing all missions...");
        CompleteGenerator();
        CompleteItem1();
        CompleteItem2();
    }
}
