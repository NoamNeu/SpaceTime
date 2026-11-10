using UnityEngine;
using UnityEngine.UI;

public class VictoryScreenController : MonoBehaviour
{
    private const float BonVoyageAspect = 1212f / 1270f;
    private const float RestartAspect = 1970f / 675f;
    private const float Spacing = 48f;

    [SerializeField] private GameObject hud;
    [SerializeField] private RectTransform bonVoyageRect;
    [SerializeField] private RectTransform restartButtonRect;
    [SerializeField] private CanvasGroup canvasGroup;

    private Button restartButton;
    private bool listenerAdded;
    private bool subscribed;
    private bool visible;
    private bool restarting;

    private void Awake()
    {
        ResolveReferences();
        ConfigureGraphics();
        EnsureRestartListener();
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
        if (!visible)
            Hide();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Show()
    {
        if (visible || restarting)
            return;

        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || !GameManager.Instance.IsTakeoffComplete))
            return;

        GameOverMenuController gameOver = FindFirstObjectByType<GameOverMenuController>();
        if (gameOver != null && gameOver.IsPresented)
            return;

        if (gameOver != null)
            gameOver.Hide();

        visible = true;
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

    public void Hide()
    {
        visible = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void RestartGame()
    {
        if (restarting)
            return;

        restarting = true;

        if (GameManager.Instance == null)
        {
            Debug.LogError("VictoryScreenController: GameManager.Instance is missing. Cannot restart.");
            restarting = false;
            return;
        }

        GameManager.Instance.RestartGame();
    }

    private void Subscribe()
    {
        if (subscribed || GameManager.Instance == null)
            return;

        GameManager.Instance.TakeoffCompleted += Show;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.TakeoffCompleted -= Show;

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

        if (bonVoyageRect == null)
        {
            Transform image = transform.Find("BonVoyage");
            if (image != null)
                bonVoyageRect = image as RectTransform;
        }

        if (restartButtonRect == null)
        {
            Transform button = transform.Find("RestartButton");
            if (button != null)
                restartButtonRect = button as RectTransform;
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        restartButton = restartButtonRect != null ? restartButtonRect.GetComponent<Button>() : null;
    }

    private void ConfigureGraphics()
    {
        ConfigureImage(bonVoyageRect, false);
        ConfigureImage(restartButtonRect, true);
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

    private void EnsureRestartListener()
    {
        if (restartButton == null || listenerAdded)
            return;

        int persistentCount = restartButton.onClick.GetPersistentEventCount();
        for (int i = 0; i < persistentCount; i++)
        {
            if (restartButton.onClick.GetPersistentTarget(i) == this
                && restartButton.onClick.GetPersistentMethodName(i) == nameof(RestartGame))
                return;
        }

        restartButton.onClick.AddListener(RestartGame);
        listenerAdded = true;
    }

    private void ApplyLayout()
    {
        if (bonVoyageRect == null || restartButtonRect == null || transform is not RectTransform parent)
            return;

        float parentWidth = parent.rect.width;
        float parentHeight = parent.rect.height;
        if (parentWidth < 10f || parentHeight < 10f)
            return;

        float margin = Mathf.Min(parentWidth, parentHeight) * 0.08f;
        float maxWidth = Mathf.Min(parentWidth - margin * 2f, 640f);
        float maxHeight = parentHeight - margin * 2f;

        float imageWidth = maxWidth;
        float imageHeight = imageWidth / BonVoyageAspect;
        float buttonWidth = imageWidth * 0.78f;
        float buttonHeight = buttonWidth / RestartAspect;
        float totalHeight = imageHeight + Spacing + buttonHeight;

        if (totalHeight > maxHeight)
        {
            float scale = (maxHeight - Spacing) / (imageHeight + buttonHeight);
            imageWidth *= scale;
            buttonWidth *= scale;
            imageHeight = imageWidth / BonVoyageAspect;
            buttonHeight = buttonWidth / RestartAspect;
            totalHeight = imageHeight + Spacing + buttonHeight;
        }

        float top = totalHeight * 0.5f;
        Place(bonVoyageRect, imageWidth, imageHeight, top);
        Place(restartButtonRect, buttonWidth, buttonHeight, top - imageHeight - Spacing);
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
        HideNamedMenu("GameOver");
        HideNamedMenu("Game Over");
        HideNamedMenu("GameOverScreen");
    }

    private void HideNamedMenu(string menuName)
    {
        GameObject menu = GameObject.Find(menuName);
        if (menu != null && menu != gameObject && !menu.transform.IsChildOf(transform))
            menu.SetActive(false);
    }

    private static bool IsNamedMenuActive(string menuName)
    {
        GameObject menu = GameObject.Find(menuName);
        return menu != null && menu.activeInHierarchy;
    }
}
