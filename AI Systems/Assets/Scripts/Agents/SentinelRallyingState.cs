using UnityEngine;

// RALLYING — Sentinel calls for The Shade when its HP is low
// Fires OnSentinelRallyingCry so Shade can enter its Rallied state
// Transitions:
//   -> Combat : cry is done, Shade has been alerted
public class SentinelRallyingState : State
{
    CryptSentinel sentinel;
    bool cryCalled;

    public SentinelRallyingState(CryptSentinel _sentinel)
    {
        sentinel = _sentinel;
    }

    public override void Enter()
    {
        // Stop and call for help
        sentinel.agent.ResetPath();
        cryCalled = false;
        Debug.Log("[Sentinel] Rallying Cry!");
    }

    public override void Execute()
    {
        if (!cryCalled)
        {
            // Fire the event — Shade is listening and will check distance
            GameEvents.OnSentinelRallyingCry?.Invoke(sentinel.transform.position);
            cryCalled = true;

            // Return to combat after cry
            sentinel.ChangeState(sentinel.combatState);
        }
    }

    public override void Exit() { }

    public override string ToString() => "Rallying";
}
