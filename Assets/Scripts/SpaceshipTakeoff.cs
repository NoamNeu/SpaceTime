using UnityEngine;

public class SpaceshipTakeoff : MonoBehaviour
{
    [Header("Takeoff")]
    [Tooltip("Upward speed, in world units per second, when takeoff begins.")]
    [Min(0f)]
    [SerializeField] private float initialSpeed = 1.5f;

    [Tooltip("How quickly that upward speed increases, in world units per second squared.")]
    [Min(0f)]
    [SerializeField] private float acceleration = 3.5f;

    private const float TriggerHeight = 2.4f;
    private const float MaxStep = 0.05f;

    private GameObject triggerObject;
    private BoxCollider2D triggerCollider;
    private float currentSpeed;
    private float distanceTraveled;
    private bool launchRequested;
    private bool sequenceRunning;
    private bool sequenceFinished;
    private bool missingManagerLogged;

    public bool IsSequenceComplete => sequenceFinished;

    private void Awake()
    {
        EnsureReachableTrigger();
    }

    private void Start()
    {
        if (!launchRequested)
            EnsureReachableTrigger();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleContact(other);
    }

    public void HandleContact(Collider2D other)
    {
        if (launchRequested || sequenceFinished || other == null)
            return;

        Player boardingPlayer = other.GetComponentInParent<Player>();
        if (boardingPlayer == null)
            return;

        if (GameManager.Instance == null)
        {
            if (!missingManagerLogged)
            {
                missingManagerLogged = true;
                Debug.LogError("SpaceshipTakeoff: GameManager.Instance is missing. Cannot start takeoff.");
            }

            return;
        }

        if (!GameManager.Instance.TryBeginTakeoff(boardingPlayer))
            return;

        launchRequested = true;
    }

    private void Update()
    {
        if (launchRequested && !sequenceRunning && !sequenceFinished)
        {
            sequenceRunning = true;
            currentSpeed = Mathf.Max(0f, initialSpeed);

            if (triggerCollider != null)
                triggerCollider.enabled = false;
        }

        if (!sequenceRunning || sequenceFinished)
            return;

        float dt = Time.unscaledDeltaTime;
        if (dt > MaxStep)
            dt = MaxStep;

        currentSpeed += Mathf.Max(0f, acceleration) * dt;
        float step = currentSpeed * dt;
        transform.position += Vector3.up * step;
        distanceTraveled += step;

        if (!HasClearedActiveCamera())
            return;

        sequenceFinished = true;
        sequenceRunning = false;

        if (GameManager.Instance != null)
            GameManager.Instance.CompleteTakeoff();
    }

    private void EnsureReachableTrigger()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || collider.isTrigger)
                continue;

            collider.enabled = false;
        }

        Transform triggerTransform = triggerObject != null ? triggerObject.transform : transform.Find("TakeoffTrigger");
        if (triggerTransform == null)
        {
            triggerObject = new GameObject("TakeoffTrigger");
            triggerTransform = triggerObject.transform;
        }
        else
        {
            triggerObject = triggerTransform.gameObject;
        }

        if (!TryGetTriggerVolume(out Vector3 center, out Vector2 worldSize))
        {
            center = transform.position;
            worldSize = new Vector2(2f, TriggerHeight);
        }

        // Keep the sensor out of the ship's scale and rotation so the player can walk into it.
        triggerTransform.SetParent(null, true);
        triggerTransform.SetPositionAndRotation(center, Quaternion.identity);
        triggerTransform.localScale = Vector3.one;

        BoxCollider2D box = triggerObject.GetComponent<BoxCollider2D>();
        if (box == null)
            box = triggerObject.AddComponent<BoxCollider2D>();

        box.isTrigger = true;
        box.size = worldSize;
        box.offset = Vector2.zero;
        box.enabled = true;
        triggerCollider = box;

        SpaceshipTakeoffSensor sensor = triggerObject.GetComponent<SpaceshipTakeoffSensor>();
        if (sensor == null)
            sensor = triggerObject.AddComponent<SpaceshipTakeoffSensor>();
        sensor.Bind(this);
    }

    private bool TryGetTriggerVolume(out Vector3 center, out Vector2 worldSize)
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
        {
            center = transform.position;
            worldSize = new Vector2(2f, TriggerHeight);
            return false;
        }

        Bounds bounds = sprite.bounds;
        float width = Mathf.Max(bounds.size.x, 1.5f);
        float bottom = bounds.min.y - 0.15f;
        center = new Vector3(bounds.center.x, bottom + TriggerHeight * 0.5f, transform.position.z);
        worldSize = new Vector2(width, TriggerHeight);
        return true;
    }

    private bool HasClearedActiveCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.isActiveAndEnabled)
            cam = FindFirstObjectByType<Camera>();

        if (cam == null)
            return distanceTraveled > TriggerHeight + 12f;

        float viewTop = cam.orthographic
            ? cam.transform.position.y + cam.orthographicSize
            : cam.transform.position.y + PerspectiveHalfHeight(cam);

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        float shipBottom = sprite != null ? sprite.bounds.min.y : transform.position.y;
        return shipBottom > viewTop;
    }

    private float PerspectiveHalfHeight(Camera cam)
    {
        float distance = Mathf.Abs(cam.transform.position.z - transform.position.z);
        if (distance < 0.01f)
            distance = 10f;

        return distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    private void OnDestroy()
    {
        if (triggerObject != null)
            Destroy(triggerObject);
    }
}

public class SpaceshipTakeoffSensor : MonoBehaviour
{
    private SpaceshipTakeoff takeoff;

    public void Bind(SpaceshipTakeoff owner)
    {
        takeoff = owner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (takeoff != null)
            takeoff.HandleContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (takeoff != null)
            takeoff.HandleContact(other);
    }
}
