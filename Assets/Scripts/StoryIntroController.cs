using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryIntroController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private TextMeshProUGUI skipText;

    [Header("Story Lines")]
    [TextArea(2, 5)]
    [SerializeField] private string[] storyLines;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float displayDuration = 2.5f;

    [Header("Next Scene")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private bool isLoadingScene;

    private void Start()
    {
        StartCoroutine(PlayStory());
    }

    private void Update()
    {
        if (Input.anyKeyDown && !isLoadingScene)
        {
            LoadGameplayScene();
        }
    }

    private IEnumerator PlayStory()
    {
        storyText.alpha = 0f;
        skipText.alpha = 0.6f;

        foreach (string line in storyLines)
        {
            storyText.text = line;

            yield return FadeText(0f, 1f);

            yield return new WaitForSeconds(displayDuration);

            yield return FadeText(1f, 0f);

            yield return new WaitForSeconds(0.3f);
        }

        LoadGameplayScene();
    }

    private IEnumerator FadeText(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            float newAlpha = Mathf.Lerp(
                startAlpha,
                endAlpha,
                elapsedTime / fadeDuration
            );

            storyText.alpha = newAlpha;

            yield return null;
        }

        storyText.alpha = endAlpha;
    }

    private void LoadGameplayScene()
    {
        if (isLoadingScene)
        {
            return;
        }

        isLoadingScene = true;

        StopAllCoroutines();

        SceneManager.LoadScene(gameplaySceneName);
    }
}