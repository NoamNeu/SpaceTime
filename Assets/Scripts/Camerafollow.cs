using UnityEngine;

public class Camerafollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 targetPosition = target.position + offset;
        targetPosition.y = transform.position.y;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}