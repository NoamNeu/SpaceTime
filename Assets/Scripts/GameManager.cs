using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum SessionState
    {
        StartScreen,
        Playing,
        GameOver,
        Takeoff,
        TakeoffComplete
    }

    public static GameManager Instance;

    /// <summary>
    /// Set once gameplay has begun. A scene reload from Restart or Try Again skips the start screen.
    /// </summary>
    public static bool SkipStartScreenOnLoad { get; private set; }

    [Header("Game Values")]
    [SerializeField] private int lives = 3;
    [SerializeField] private int score = 0;
    [SerializeField] private int coins = 0;

    [Header("UI Text")]
    [SerializeField] private TMP_Text lifeText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinText;

    [Header("Player")]
    [SerializeField] private Player player;

    private Vector3 playerSpawnPosition;
    private bool isHandlingLifeLoss;
    private bool boardPlayerOnNextUpdate;
    private bool restartRequested;

    public SessionState State { get; private set; } = SessionState.Playing;

    public bool IsGameOver => State == SessionState.GameOver;
    public bool IsStartScreenActive => State == SessionState.StartScreen || IsSceneStartScreenActive();
    public bool IsTakeoffComplete => State == SessionState.TakeoffComplete;
    public bool AllowsDamage => State == SessionState.Playing;
    public bool AllowsItemCollection => State == SessionState.Playing;

    /// <summary>
    /// Fired once, after the spaceship has left the camera view. The victory screen subscribes here.
    /// </summary>
    public event Action TakeoffCompleted;

    /// <summary>
    /// Fired after the heart counter has been set to 0. The Game Over screen subscribes here.
    /// </summary>
    public event Action GameOverReached;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (player == null)
            Debug.LogError("GameManager: Player reference is missing. Assign the Player GameObject to the Player field.");
        else
            playerSpawnPosition = player.transform.position;

        if (lifeText == null)
            Debug.LogError("GameManager: lifeText is not assigned. Assign Canvas/TopBar/LifeText.");
        if (scoreText == null)
            Debug.LogError("GameManager: scoreText is not assigned. Assign Canvas/TopBar/ScoreText (ghost counter).");
        if (coinText == null)
            Debug.LogError("GameManager: coinText is not assigned. Assign Canvas/TopBar/CoinText.");

        if (!SkipStartScreenOnLoad && IsSceneStartScreenActive())
            SetStartScreenActive(true);

        UpdateUI();
    }

    private void Update()
    {
        if (!boardPlayerOnNextUpdate)
            return;

        boardPlayerOnNextUpdate = false;
        Time.timeScale = 0f;

        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (player != null)
            player.PrepareForTakeoff();
    }

    public void SetStartScreenActive(bool active)
    {
        if (State == SessionState.GameOver || State == SessionState.Takeoff || State == SessionState.TakeoffComplete)
            return;

        if (!active)
            SkipStartScreenOnLoad = true;

        State = active ? SessionState.StartScreen : SessionState.Playing;
    }

    public bool CanBeginTakeoff()
    {
        return State == SessionState.Playing && !IsSceneStartScreenActive();
    }

    public bool TryBeginTakeoff(Player boardingPlayer)
    {
        if (!CanBeginTakeoff())
            return false;

        State = SessionState.Takeoff;

        if (boardingPlayer != null)
            player = boardingPlayer;

        // Applied next Update, outside the physics callback. Scaled time stops; the ship uses unscaled time.
        boardPlayerOnNextUpdate = true;

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlaySpaceshipTakeoff();

        return true;
    }

    public void CompleteTakeoff()
    {
        if (State != SessionState.Takeoff)
            return;

        State = SessionState.TakeoffComplete;
        TakeoffCompleted?.Invoke();
    }

    /// <summary>
    /// Reloads the scene as a fresh game and skips the start screen.
    /// </summary>
    public void RestartGame()
    {
        if (restartRequested)
            return;

        restartRequested = true;
        SkipStartScreenOnLoad = true;
        Time.timeScale = 1f;
        Player.ResetSceneSetupForReload();

        Scene active = SceneManager.GetActiveScene();
        if (active.buildIndex >= 0)
            SceneManager.LoadScene(active.buildIndex);
        else
            SceneManager.LoadScene(active.name);
    }

    public void AddScore(int amount)
    {
        if (State != SessionState.Playing)
            return;

        score += amount;
        UpdateUI();
    }

    public void AddCoin(int amount)
    {
        if (State != SessionState.Playing)
            return;

        coins += amount;
        UpdateUI();

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayCoinPickup();
    }

    public void LoseLife()
    {
        if (!AllowsDamage || isHandlingLifeLoss || lives <= 0)
            return;

        isHandlingLifeLoss = true;

        lives--;
        if (lives < 0)
            lives = 0;

        UpdateUI();

        if (lives <= 0)
            HandleGameOver();
        else
            RespawnPlayer();

        isHandlingLifeLoss = false;
    }

    private void RespawnPlayer()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (player == null)
        {
            Debug.LogError("GameManager: Cannot respawn — Player is missing.");
            return;
        }

        player.RespawnAt(playerSpawnPosition);
    }

    private void HandleGameOver()
    {
        if (State == SessionState.GameOver)
            return;

        State = SessionState.GameOver;
        Time.timeScale = 0f;

        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (player != null)
            player.SetGameplayEnabled(false);

        VictoryScreenController victory = FindFirstObjectByType<VictoryScreenController>();
        if (victory != null)
            victory.Hide();

        GameOverReached?.Invoke();
    }

    private void UpdateUI()
    {
        if (lifeText != null)
            lifeText.text = "× " + lives;

        if (scoreText != null)
            scoreText.text = "× " + score;

        if (coinText != null)
            coinText.text = "× " + coins;
    }

    private static bool IsSceneStartScreenActive()
    {
        string[] names = { "StartScreen", "Start Menu", "StartMenu" };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject screen = GameObject.Find(names[i]);
            if (screen != null && screen.activeInHierarchy)
                return true;
        }

        return false;
    }
}
