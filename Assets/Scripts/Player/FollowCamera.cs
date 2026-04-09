using UnityEngine;

// Smoothly follows the player from a top-down angle
// Attach this script to the Main Camera
public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 20f, -10f);
    public float smoothSpeed = 8f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
    }
}
