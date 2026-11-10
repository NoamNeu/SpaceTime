using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public float speed = 2f;
    public Transform[] points;

    private int i;

    void Start()
    {
        transform.position = points[0].position;
    }

    void Update()
    {
        if (GameManager.Instance != null &&
            (GameManager.Instance.State == GameManager.SessionState.StartScreen ||
             GameManager.Instance.State == GameManager.SessionState.GameOver))
            return;

        if(Vector2.Distance(transform.position, points[i].position) < 0.1f)
        {
            i++;
            if(i == points.Length)
            {
                i = 0;
            }
        }
        transform.position = Vector2.MoveTowards(transform.position, points[i].position, speed * Time.deltaTime);
        
    }
}
