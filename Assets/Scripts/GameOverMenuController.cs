using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameOverMenuController : MonoBehaviour
{
    private const float GameOverAspect = 1188f / 1270f;
    private const float TryAgainAspect = 1800f / 637f;
    private const float Spacing = 48f;
    private const float CounterVisibleSeconds = 0.6f;

    [SerializeField] private GameObject hud;
    [SerializeField] private RectTransform gameOverRect;
    [SerializeField] private RectTransform tryAgainButtonRect;
    [SerializeField] private CanvasGroup canvasGroup;

    private Button tryAgainButton;
    private bool listenerAdded;
    private bool subscribed;
    private bool queued;
    private bool presented;
    private bool restarting;

    public bool IsPresented => presented;

    private void Awake()
    {
        ResolveReferences();
        ConfigureGraphics();
        EnsureTryAgainListener();
        ApplyLayout();
        Hide();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        ResolveReferences();
        Subscribe();
        ApplyLayout();
        if (!presented)
            Hide();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Show()
    {
        if (queued || presented || restarting)
            return;

        if (GameManager.Instance != null &&
            (GameManager.Instance.State == GameManager.SessionState.Takeoff ||
             GameManager.Instance.IsTakeoffComplete))
            return;

        queued = true;
        StartCoroutine(ShowAfterCounter());
    }

    public void Hide()
    {
        queued = false;
        presented = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void TryAgain()
    {
        if (restarting)
            return;

        restarting = true;

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameOverMenuController: GameManager.Instance is missing. Cannot restart.");
            restarting = false;
            return;
        }

        GameManager.Instance.RestartGame();
    }

    private IEnumerator ShowAfterCounter()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForSecondsRealtime(CounterVisibleSeconds);

        if (!queued || restarting)
            yield break;

        if (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
        {
            queued = false;
            yield break;
        }

        if (GameManager.Instance.IsTakeoffComplete || GameManager.Instance.State == GameManager.SessionState.Takeoff)
        {
            queued = false;
            yield break;
        }

        queued = false;
        presented = true;
        HideOtherMenus();
        HideHud();
        ApplyLayout();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        Time.timeScale = 0f;
    }

    private void Subscribe()
    {
        if (subscribed || GameManager.Instance == null)
            return;

        GameManager.Instance.GameOverReached += Show;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.GameOverReached -= Show;

        subscribed = false;
    }

    private void ResolveReferences()
    {
        if (hud == null)
        {
            GameObject topBar = GameObject.Find("TopBar");
            if (topBar != null)
                hud = topBar;
        }

        if (gameOverRect == null)
        {
            Transform image = transform.Find("GameOverImage");
            if (image != null)
                gameOverRect = image as RectTransform;
        }

        if (tryAgainButtonRect == null)
        {
            Transform button = transform.Find("TryAgainButton");
            if (button != null)
                tryAgainButtonRect = button as RectTransform;
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        tryAgainButton = tryAgainButtonRect != null ? tryAgainButtonRect.GetComponent<Button>() : null;
    }

    private void ConfigureGraphics()
    {
        ConfigureImage(gameOverRect, false);
        ConfigureImage(tryAgainButtonRect, true);
    }

    private static void ConfigureImage(RectTransform rect, bool raycastTarget)
    {
        if (rect == null || !rect.TryGetComponent(out Image image))
            return;

        image.preserveAspect = true;
        image.raycastTarget = raycastTarget;
        image.type = Image.Type.Simple;
        image.color = Color.white;
    }

    private void EnsureTryAgainListener()
    {
        if (tryAgainButton == null || listenerAdded)
            return;

        int persistentCount = tryAgainButton.onClick.GetPersistentEventCount();
        for (int i = 0; i < persistentCount; i++)
        {
            if (tryAgainButton.onClick.GetPersistentTarget(i) == this
                && tryAgainButton.onClick.GetPersistentMethodName(i) == nameof(TryAgain))
                return;
        }

        tryAgainButton.onClick.AddListener(TryAgain);
        listenerAdded = true;
    }

    private void ApplyLayout()
    {
        if (gameOverRect == null || tryAgainButtonRect == null || transform is not RectTransform parent)
            return;

        float parentWidth = parent.rect.width;
        float parentHeight = parent.rect.height;
        if (parentWidth < 10f || parentHeight < 10f)
            return;

        float margin = Mathf.Min(parentWidth, parentHeight) * 0.08f;
        float maxWidth = Mathf.Min(parentWidth - margin * 2f, 640f);
        float maxHeight = parentHeight - margin * 2f;

        float imageWidth = maxWidth;
        float imageHeight = imageWidth / GameOverAspect;
        float buttonWidth = imageWidth * 0.78f;
        float buttonHeight = buttonWidth / TryAgainAspect;
        float totalHeight = imageHeight + Spacing + buttonHeight;

        if (totalHeight > maxHeight)
        {
            float scale = (maxHeight - Spacing) / (imageHeight + buttonHeight);
            imageWidth *= scale;
            buttonWidth *= scale;
            imageHeight = imageWidth / GameOverAspect;
            buttonHeight = buttonWidth / TryAgainAspect;
            totalHeight = imageHeight + Spacing + buttonHeight;
        }

        float top = totalHeight * 0.5f;
        Place(gameOverRect, imageWidth, imageHeight, top);
        Place(tryAgainButtonRect, buttonWidth, buttonHeight, top - imageHeight - Spacing);
    }

    private static void Place(RectTransform rect, float width, float height, float top)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void HideHud()
    {
        if (hud == null)
        {
            GameObject topBar = GameObject.Find("TopBar");
            if (topBar != null)
                hud = topBar;
        }

        if (hud != null && hud.activeSelf)
            hud.SetActive(false);
    }

    private void HideOtherMenus()
    {
        HideNamedMenu("StartScreen");
        HideNamedMenu("Start Menu");
        HideNamedMenu("StartMenu");
        HideNamedMenu("VictoryScreen");

        VictoryScreenController victory = FindFirstObjectByType<VictoryScreenController>();
        if (victory != null)
            victory.Hide();
    }

    private void HideNamedMenu(string menuName)
    {
        GameObject menu = GameObject.Find(menuName);
        if (menu != null && menu != gameObject && !menu.transform.IsChildOf(transform))
            menu.SetActive(false);
    }
}
