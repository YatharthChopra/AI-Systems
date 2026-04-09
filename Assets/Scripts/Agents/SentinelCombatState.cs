using UnityEngine;

// COMBAT — Sentinel charges and attacks the player at melee range
// Transitions:
//   -> Staggered  : player lands a hit (event-driven)
//   -> Rallying   : HP drops below threshold (event-driven)
//   -> Return     : loses sight for longer than memoryDuration
public class SentinelCombatState : State
{
    CryptSentinel sentinel;
    float sightLostTimer;
    bool sightLost;
    float attackCooldown = 1.5f;  // seconds between attacks
    float attackTimer;

    public SentinelCombatState(CryptSentinel _sentinel)
    {
        sentinel = _sentinel;
    }

    public override void Enter()
    {
        sentinel.agent.speed = sentinel.chargeSpeed;
        sightLostTimer = 0f;
        sightLost = false;
        attackTimer = 0f;
    }

    public override void Execute()
    {
        if (sentinel.playerTransform == null) return;

        bool canSee = sentinel.vision.CanSee(sentinel.playerTransform);

        if (canSee)
        {
            // Chase the player and keep last known position updated
            sightLost = false;
            sightLostTimer = 0f;
            sentinel.lastKnownPlayerPos = sentinel.playerTransform.position;
            sentinel.agent.SetDestination(sentinel.playerTransform.position);

            // Attack if close enough and cooldown has expired
            float dist = Vector3.Distance(sentinel.transform.position, sentinel.playerTransform.position);
            attackTimer -= Time.deltaTime;
            if (dist <= sentinel.attackRange && attackTimer <= 0f)
            {
                Attack();
                attackTimer = attackCooldown;
            }
        }
        else
        {
            // Head to last known position and start the memory countdown
            if (!sightLost)
            {
                sightLost = true;
                sentinel.agent.SetDestination(sentinel.lastKnownPlayerPos);
            }

            sightLostTimer += Time.deltaTime;
            if (sightLostTimer >= sentinel.memoryDuration)
                sentinel.ChangeState(sentinel.returnState);
        }
    }

    public override void Exit()
    {
        // Reset speed so other states don't inherit charge speed
        sentinel.agent.speed = sentinel.patrolSpeed;
    }

    void Attack()
    {
        // In a full implementation this triggers an animation and hitbox
        // For now we log so it's visible in the editor during testing
        Debug.Log("[Sentinel] Greatsword attack!");
    }

    public override string ToString() => "Combat";
}
