using UnityEngine;

public class Coin : MonoBehaviour
{
    private bool collected;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collected)
            return;

        if(collision.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player == null || !player.CanCollectItems)
                return;

            collected = true;
            player.coins += 1;
            GameManager.Instance.AddCoin(1);
            Destroy(gameObject);
        }
    }
}
