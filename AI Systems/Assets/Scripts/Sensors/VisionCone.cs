using UnityEngine;

// Handles vision cone detection for the Crypt Sentinel
// Uses a distance check, angle check, then a raycast to confirm line of sight
public class VisionCone : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewRadius = 9f;
    [Range(0, 360)]
    public float viewAngle = 60f;

    // Whether CanSee returned true on the last call — read by VisionConeMesh to colour the fan
    public bool IsDetecting { get; private set; }

    // --- DETECTION ---

    // Returns true if the target is inside the cone and not behind a wall
    public bool CanSee(Transform target)
    {
        if (target == null) { IsDetecting = false; return false; }

        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        // 1. Distance check
        if (distance > viewRadius) { IsDetecting = false; return false; }

        // 2. Angle check
        float angle = Vector3.Angle(transform.forward, toTarget.normalized);
        if (angle > viewAngle * 0.5f) { IsDetecting = false; return false; }

        // 3. Raycast — make sure nothing is blocking line of sight
        if (Physics.Raycast(transform.position, toTarget.normalized, out RaycastHit hit, viewRadius))
        {
            IsDetecting = hit.transform == target;
            return IsDetecting;
        }

        IsDetecting = false;
        return false;
    }

    // --- EDITOR VISUALISATION ---

    // OnDrawGizmos (not Selected) so the cone is always visible during Play mode
    // Cyan = searching, Red = player detected — matches the prof's demo convention
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = IsDetecting
            ? new Color(1f, 0f, 0f, 0.25f)
            : new Color(0f, 1f, 1f, 0.25f);

        UnityEditor.Handles.DrawSolidArc(
            transform.position, Vector3.up, transform.forward,  viewAngle * 0.5f, viewRadius);
        UnityEditor.Handles.DrawSolidArc(
            transform.position, Vector3.up, transform.forward, -viewAngle * 0.5f, viewRadius);
#endif
    }
}
