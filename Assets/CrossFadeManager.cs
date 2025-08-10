using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// Singleton
/// </summary>
public class CrossfadeManager : MonoBehaviour
{
    public static CrossfadeManager Instance
    {
        get; private set;
    }

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create a root canvas
        GameObject canvasGO = new GameObject("CrossfadeCanvas");
        canvasGO.transform.SetParent(this.transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // High sort order to be on top of everything

        // Add the CanvasGroup for fading
        this.canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        this.canvasGroup.alpha = 0f; // Start fully transparent

        // Create a full-screen black image
        Image image = new GameObject("CrossfadeImage").AddComponent<Image>();
        image.transform.SetParent(canvasGO.transform, false);
        image.color = Color.black;

        // Make the image stretch to fill the entire screen
        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Fades the screen TO black.
    /// </summary>
    public Tween FadeToBlack(float duration = 1f)
    {
        // Don't allow clicks during the fade
        canvasGroup.blocksRaycasts = true;
        return canvasGroup.DOFade(1f, duration);
    }

    /// <summary>
    /// Fades the screen FROM black.
    /// </summary>
    public Tween FadeFromBlack(float duration = 1f)
    {
        // Return a tween so the caller can wait for completion
        return canvasGroup.DOFade(0f, duration).OnComplete(() => {
            // Re-enable clicks after fading out
            canvasGroup.blocksRaycasts = false;
        });
    }
}
