using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartScreenController : MonoBehaviour
{
    [SerializeField] private GameObject hud;
    [SerializeField] private Player player;
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private RectTransform startButtonRect;

    private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    private readonly List<Animator> backgroundAnimators = new List<Animator>();
    private readonly List<AnimatorUpdateMode> backgroundAnimatorModes = new List<AnimatorUpdateMode>();

    private Button startButton;
    private bool gameStarted;
    private bool hudHidden;
    private bool caughtFirstFrame;

    private void Awake()
    {
        if (GameManager.SkipStartScreenOnLoad)
        {
            Time.timeScale = 1f;
            gameObject.SetActive(false);
            return;
        }

        ResolveReferences();
        ConfigureGraphics();
        startButton = startButtonRect != null ? startButtonRect.GetComponent<Button>() : null;
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        HideLevelVisuals();
        PauseGameplay();
    }

    private void Start()
    {
        if (gameStarted || GameManager.SkipStartScreenOnLoad)
            return;

        ResolveReferences();
        HideLevelVisuals();
        PauseGameplay();
    }

    private void LateUpdate()
    {
        if (caughtFirstFrame || gameStarted)
            return;

        caughtFirstFrame = true;
        HideLevelVisuals();
        HideHud();
    }

    public void StartGame()
    {
        if (gameStarted)
            return;

        gameStarted = true;
        caughtFirstFrame = true;

        ShowLevelVisuals();
        ShowHud();

        if (GameManager.Instance != null)
            GameManager.Instance.SetStartScreenActive(false);

        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (player != null)
            player.SetGameplayEnabled(true);

        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (hud == null)
        {
            GameObject topBar = GameObject.Find("TopBar");
            if (topBar != null)
                hud = topBar;
        }

        if (titleRect == null)
        {
            Transform title = transform.Find("Title");
            if (title != null)
                titleRect = title as RectTransform;
        }

        if (startButtonRect == null)
        {
            Transform button = transform.Find("StartButton");
            if (button != null)
                startButtonRect = button as RectTransform;
        }

        if (player == null)
            player = FindFirstObjectByType<Player>();
    }

    private void ConfigureGraphics()
    {
        ConfigureImage(titleRect, false);
        ConfigureImage(startButtonRect, true);
    }

    private static void ConfigureImage(RectTransform rect, bool raycastTarget)
    {
        if (rect == null || !rect.TryGetComponent(out Image image))
            return;

        image.preserveAspect = true;
        image.raycastTarget = raycastTarget;
        image.type = Image.Type.Simple;
    }

    private void PauseGameplay()
    {
        Time.timeScale = 0f;

        GameManager manager = GameManager.Instance != null
            ? GameManager.Instance
            : FindFirstObjectByType<GameManager>();

        if (manager != null)
            manager.SetStartScreenActive(true);

        if (player != null)
            player.SetGameplayEnabled(false);

        HideHud();
    }

    private void HideHud()
    {
        if (hud == null || hudHidden || !hud.activeSelf)
            return;

        hud.SetActive(false);
        hudHidden = true;
    }

    private void ShowHud()
    {
        if (hud == null || !hudHidden)
            return;

        hud.SetActive(true);
        hudHidden = false;
    }

    private void HideLevelVisuals()
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !ShouldHide(renderer.transform))
                continue;

            renderer.enabled = false;
            hiddenRenderers.Add(renderer);
        }

        KeepBackgroundAnimationRunning();
    }

    private void ShowLevelVisuals()
    {
        for (int i = 0; i < hiddenRenderers.Count; i++)
        {
            if (hiddenRenderers[i] != null)
                hiddenRenderers[i].enabled = true;
        }

        hiddenRenderers.Clear();
        RestoreBackgroundAnimators();
    }

    private void KeepBackgroundAnimationRunning()
    {
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null || !IsSpaceBackground(animator.transform) || backgroundAnimators.Contains(animator))
                continue;

            backgroundAnimators.Add(animator);
            backgroundAnimatorModes.Add(animator.updateMode);
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }

    private void RestoreBackgroundAnimators()
    {
        for (int i = 0; i < backgroundAnimators.Count; i++)
        {
            if (backgroundAnimators[i] != null)
                backgroundAnimators[i].updateMode = backgroundAnimatorModes[i];
        }

        backgroundAnimators.Clear();
        backgroundAnimatorModes.Clear();
    }

    private bool ShouldHide(Transform target)
    {
        if (target == null || target.GetComponentInParent<Canvas>() != null)
            return false;

        return !IsSpaceBackground(target);
    }

    private static bool IsSpaceBackground(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (current.name.StartsWith("Earth-from-space"))
                return true;

            current = current.parent;
        }

        return false;
    }
}
