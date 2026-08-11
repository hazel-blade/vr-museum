using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingSceneController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject thanksForPlayingText;
    public GameObject mainMenuButton;

    [Header("Settings")]
    public float delayToShowThanks = 1f;
    public float delayToShowButton = 2f;
    public string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        // Hide initially
        if (thanksForPlayingText != null)
            thanksForPlayingText.SetActive(false);
        
        if (mainMenuButton != null)
            mainMenuButton.SetActive(false);

        StartCoroutine(EndingSequence());
    }

    private IEnumerator EndingSequence()
    {
        // Wait before showing Thanks text
        yield return new WaitForSeconds(delayToShowThanks);
        if (thanksForPlayingText != null)
            thanksForPlayingText.SetActive(true);

        // Wait before showing Button
        yield return new WaitForSeconds(delayToShowButton);
        if (mainMenuButton != null)
            mainMenuButton.SetActive(true);
    }

    public void OnMainMenuButtonClicked()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
