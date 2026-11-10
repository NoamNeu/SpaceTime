using UnityEngine;

public class Enemy : MonoBehaviour
{
    private bool scoredThisContact;

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    private void EnsureTriggerCollider()
    {
        Collider2D[] existing = GetComponentsInChildren<Collider2D>(true);
        if (existing.Length > 0)
        {
            for (int i = 0; i < existing.Length; i++)
                existing[i].isTrigger = true;
            return;
        }

        BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryRegisterHit(collision != null ? collision.gameObject : null);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryRegisterHit(collision != null ? collision.gameObject : null);
    }

    public void TryRegisterHit(GameObject other)
    {
        if (scoredThisContact || other == null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.SessionState.Playing)
            return;

        if (other.GetComponentInParent<Player>() == null)
            return;

        scoredThisContact = true;

        if (GameManager.Instance == null)
        {
            Debug.LogError("Enemy: GameManager.Instance is missing.");
            return;
        }

        GameManager.Instance.AddScore(1);

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayGhostHit();

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        gameObject.SetActive(false);
    }
}
