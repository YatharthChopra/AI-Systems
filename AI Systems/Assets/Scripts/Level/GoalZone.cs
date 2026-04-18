using UnityEngine;

// The exit zone — player enters this trigger to win the game
// Fires GameEvents.OnPlayerEscaped so GameManager can handle the win screen
[RequireComponent(typeof(SphereCollider))]
public class GoalZone : MonoBehaviour
{
    void Awake()
    {
        // Make sure the collider is a trigger, not a solid blocker
        GetComponent<SphereCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            Debug.Log("[GoalZone] Player reached the exit!");
            GameEvents.OnPlayerEscaped?.Invoke();
        }
    }
}
