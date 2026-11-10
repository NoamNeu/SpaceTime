using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class Player : MonoBehaviour
{
    public int coins = 0;
    public int health = 100;
    public float speed = 5f;
    public float jumpForce = 6f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool jumpRequested;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D[] ownColliders;

    [Tooltip("Enable if the sprite faces left at positive localScale.x in the original asset.")]
    [SerializeField] private bool spriteFlippedByDefault = false;

    [Header("Fall / Death")]
    [SerializeField] private float fallYLimit = -8f;

    private bool isFacingRight = true;
    private Transform flipTransform;
    private float flipScaleMagnitude = 1f;
    private Vector2 moveInput;
    private bool fallLifeLost;
    private bool gameplayEnabled = true;
    private bool damageEnabled = true;
    private bool collectionEnabled = true;

    public bool CanTakeDamage =>
        gameplayEnabled &&
        damageEnabled &&
        (GameManager.Instance == null || GameManager.Instance.AllowsDamage);

    public bool CanCollectItems =>
        gameplayEnabled &&
        collectionEnabled &&
        (GameManager.Instance == null || GameManager.Instance.AllowsItemCollection);

    private static bool sceneGroundPhysicsConfigured;

    public static void ResetSceneSetupForReload()
    {
        sceneGroundPhysicsConfigured = false;
    }

    void Awake()
    {
        EnsurePlayerPhysics();

        animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;
    }

    void Start()
    {
        EnsureSceneGroundPhysics();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (groundCheck == null)
        {
            var groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            groundCheck = groundCheckObj.transform;
        }

        if (groundLayer.value == 0)
        {
            int groundLayerIndex = LayerMask.NameToLayer("Ground");
            if (groundLayerIndex >= 0)
                groundLayer = 1 << groundLayerIndex;
        }

        EnsureValidRootScale();

        flipTransform = spriteRenderer != null ? spriteRenderer.transform : transform;
        flipScaleMagnitude = Mathf.Abs(flipTransform.localScale.x);
        if (flipScaleMagnitude < Mathf.Epsilon)
            flipScaleMagnitude = Mathf.Max(Mathf.Abs(transform.localScale.y), Mathf.Abs(transform.localScale.z), 0.5f);

        if (spriteRenderer != null)
            spriteRenderer.flipX = false;

        transform.rotation = Quaternion.identity;
        ApplyFacingScale(isFacingRight);
        RefitSolidColliderToVisual();
        rb.simulated = true;

        if (!GameManager.SkipStartScreenOnLoad
            && GameManager.Instance != null
            && GameManager.Instance.IsStartScreenActive)
            SetGameplayEnabled(false);
    }

    void Update()
    {
        if (!gameplayEnabled)
        {
            moveInput = Vector2.zero;
            return;
        }

        CheckFallBelowMap();

        if (moveInput.x > 0f && !isFacingRight)
            Flip(true);
        else if (moveInput.x < 0f && isFacingRight)
            Flip(false);

        // setAnimation(moveInput.x);
    }

    void FixedUpdate()
    {
        if (!gameplayEnabled)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 checkPosition = GetGroundCheckPosition();
        isGrounded = false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPosition, groundCheckRadius, groundLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null || IsOwnCollider(hits[i]))
                continue;

            isGrounded = true;
            break;
        }

        if (isGrounded && rb.linearVelocity.y > 0.05f)
            isGrounded = false;

        if (jumpRequested && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isGrounded = false;
        }

        jumpRequested = false;

        float xSpeed = Mathf.Abs(speed);
        float ySpeed = isGrounded ? 0f : rb.linearVelocity.y;
        rb.linearVelocity = new Vector2(moveInput.x * xSpeed, ySpeed);
    }

    void OnMove(InputValue value)
    {
        if (!gameplayEnabled)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = value.Get<Vector2>();
    }

    void OnJump()
    {
        if (!gameplayEnabled)
            return;

        jumpRequested = true;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision.gameObject.tag == "Coin")
        {
            if (CanCollectItems)
                Destroy(collision.gameObject);
        }

        Enemy enemy = collision != null ? collision.GetComponentInParent<Enemy>() : null;
        if (enemy != null)
            enemy.TryRegisterHit(gameObject);
    }

    public void RespawnAt(Vector3 position)
    {
        transform.position = position;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        fallLifeLost = false;
        jumpRequested = false;
        moveInput = Vector2.zero;
    }

    public void SetGameplayEnabled(bool enabled)
    {
        gameplayEnabled = enabled;
        moveInput = Vector2.zero;
        jumpRequested = false;

        if (rb == null)
            return;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = enabled;
    }

    public void PrepareForTakeoff()
    {
        damageEnabled = false;
        collectionEnabled = false;
        SetGameplayEnabled(false);
        HideCharacter();
    }

    private void HideCharacter()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = false;
        }

        Animator[] animators = GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
                animators[i].enabled = false;
        }
    }

    private void CheckFallBelowMap()
    {
        if (!CanTakeDamage)
            return;

        if (transform.position.y >= fallYLimit)
        {
            fallLifeLost = false;
            return;
        }

        if (fallLifeLost)
            return;

        fallLifeLost = true;

        if (GameManager.Instance == null)
        {
            Debug.LogError("Player: GameManager.Instance is missing. Cannot apply fall life loss.");
            return;
        }

        GameManager.Instance.LoseLife();
    }

    private void EnsurePlayerPhysics()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = false;
        if (rb.gravityScale <= 0f)
            rb.gravityScale = 2f;

        rb.includeLayers = Physics2D.AllLayers;
        rb.excludeLayers = 0;

        Collider2D[] colliders = GetComponents<Collider2D>();
        Collider2D solidCollider = null;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && !colliders[i].isTrigger)
            {
                solidCollider = colliders[i];
                break;
            }
        }

        if (solidCollider == null)
            solidCollider = gameObject.AddComponent<BoxCollider2D>();

        DisableChildPhysicsColliders();
        DisableMisalignedTriggerColliders();

        ownColliders = GetComponentsInChildren<Collider2D>();
    }

    private void RefitSolidColliderToVisual()
    {
        if (TryGetComponent(out BoxCollider2D boxCollider))
            FitBoxColliderToSprite(boxCollider);
    }

    private void DisableChildPhysicsColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject == gameObject)
                continue;

            colliders[i].enabled = false;
        }
    }

    private void DisableMisalignedTriggerColliders()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].isTrigger)
                continue;

            if (colliders[i].offset.sqrMagnitude > 4f)
                colliders[i].enabled = false;
        }
    }

    private void FitBoxColliderToSprite(BoxCollider2D box)
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
            return;

        box.isTrigger = false;

        Bounds spriteBounds = sprite.sprite.bounds;
        Transform spriteTransform = sprite.transform;
        Vector2 localMin = transform.InverseTransformPoint(spriteTransform.TransformPoint(spriteBounds.min));
        Vector2 localMax = transform.InverseTransformPoint(spriteTransform.TransformPoint(spriteBounds.max));
        box.offset = (localMin + localMax) * 0.5f;
        box.size = new Vector2(Mathf.Abs(localMax.x - localMin.x), Mathf.Abs(localMax.y - localMin.y));
    }

    private static void EnsureSceneGroundPhysics()
    {
        if (sceneGroundPhysicsConfigured)
            return;

        sceneGroundPhysicsConfigured = true;

        TilemapCollider2D[] tilemapColliders = FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None);
        for (int i = 0; i < tilemapColliders.Length; i++)
            ConfigureTilemapGround(tilemapColliders[i]);
    }

    private static void ConfigureTilemapGround(TilemapCollider2D tilemapCollider)
    {
        if (tilemapCollider == null)
            return;

        GameObject groundObject = tilemapCollider.gameObject;
        tilemapCollider.isTrigger = false;
        tilemapCollider.usedByComposite = true;

        CompositeCollider2D composite = groundObject.GetComponent<CompositeCollider2D>();
        if (composite == null)
            composite = groundObject.AddComponent<CompositeCollider2D>();
        composite.isTrigger = false;
        composite.geometryType = CompositeCollider2D.GeometryType.Outlines;

        Rigidbody2D groundBody = groundObject.GetComponent<Rigidbody2D>();
        if (groundBody == null)
            groundBody = groundObject.AddComponent<Rigidbody2D>();
        groundBody.bodyType = RigidbodyType2D.Static;
        groundBody.simulated = true;
    }

    private bool IsOwnCollider(Collider2D collider)
    {
        if (collider.attachedRigidbody == rb)
            return true;

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == collider)
                return true;
        }

        return false;
    }

    private Vector2 GetGroundCheckPosition()
    {
        if (groundCheck != null)
            return groundCheck.position;

        if (spriteRenderer != null)
            return new Vector2(spriteRenderer.bounds.center.x, spriteRenderer.bounds.min.y);

        return (Vector2)transform.position + Vector2.down * 0.5f;
    }

    private void Flip(bool faceRight)
    {
        if (isFacingRight == faceRight)
            return;

        isFacingRight = faceRight;
        ApplyFacingScale(faceRight);
    }

    private void EnsureValidRootScale()
    {
        Vector3 scale = transform.localScale;
        if (Mathf.Abs(scale.x) >= Mathf.Epsilon)
            return;

        float fallback = Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z), 0.5f);
        scale.x = fallback;
        transform.localScale = scale;
    }

    private void ApplyFacingScale(bool faceRight)
    {
        float sign = faceRight ? 1f : -1f;
        if (spriteFlippedByDefault)
            sign *= -1f;

        Vector3 scale = flipTransform.localScale;
        scale.x = sign * flipScaleMagnitude;
        flipTransform.localScale = scale;
    }

    private void SetAnimation(float moveInput)
    {
        if (moveInput == 0)
        {
            animator.Play("player_idle");
        }
        else
        {
            animator.Play("player_run");
        }
        // else
        //{
        //    animator.SetBool("isRunning", false);
        //}
        // else
        //{
        //    if (rb.linearVelocity.y > 0)
        //    {
        //        animator.Play("player_jump");
        //    }
        //    else
        //    {
        //        animator.Play("player_fall");
        //    }
        //}
    }

    private void OnDrawGizmos()
    {
        Vector2 checkPosition = GetGroundCheckPosition();

        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawSphere(checkPosition, groundCheckRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
    }
}


