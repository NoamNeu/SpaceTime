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
            collected = true;
            Player player = collision.gameObject.GetComponent<Player>();
            player.coins += 1;
            GameManager.Instance.AddCoin(1);
            Destroy(gameObject);
        }
    }
}
