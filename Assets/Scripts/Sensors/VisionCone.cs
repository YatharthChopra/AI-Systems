using UnityEngine;

// Handles vision cone detection for the Crypt Sentinel
// Uses a distance check, angle check, then a raycast to confirm line of sight
public class VisionCone : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewRadius = 9f;
    [Range(0, 360)]
    public float viewAngle = 60f;

    // Returns true if the target is inside the cone and not behind a wall
    public bool CanSee(Transform target)
    {
        if (target == null) return false;

        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        // 1. Distance check
        if (distance > viewRadius) return false;

        // 2. Angle check
        float angle = Vector3.Angle(transform.forward, toTarget.normalized);
        if (angle > viewAngle * 0.5f) return false;

        // 3. Raycast — make sure nothing is blocking line of sight
        if (Physics.Raycast(transform.position, toTarget.normalized, out RaycastHit hit, viewRadius))
        {
            return hit.transform == target;
        }

        return false;
    }

    // Draw the cone in the editor for easy debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 leftDir  = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0,  viewAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftDir  * viewRadius);
        Gizmos.DrawRay(transform.position, rightDir * viewRadius);
        Gizmos.DrawRay(transform.position, transform.forward * viewRadius);
    }
}
