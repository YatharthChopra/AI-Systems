using UnityEngine;

// Handles the player's melee attack against the Crypt Sentinel
// Left-click fires a short raycast forward; hitting the Sentinel triggers the stagger event
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    public float attackRange = 2f;
    public float attackDamage = 20f;

    // --- UNITY LIFECYCLE ---

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryAttack();
    }

    // --- ATTACK LOGIC ---

    void TryAttack()
    {
        // Cast from mid-torso forward so we don't clip the floor
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, attackRange))
        {
            CryptSentinel target = hit.transform.GetComponent<CryptSentinel>();
            if (target != null)
            {
                target.TakeDamage(attackDamage);
                GameEvents.OnPlayerHitSentinel?.Invoke();
                Debug.Log("[Player] Hit Sentinel!");
            }
        }
    }

    // --- EDITOR VISUALISATION ---

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * attackRange);
    }
}
