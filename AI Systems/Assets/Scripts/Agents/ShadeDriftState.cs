using UnityEngine;

// DRIFT — Shade floats slowly around the room perimeter, listening for sounds
// Transitions:
//   -> Alert : picks up a sound within soundRadius
public class ShadeDriftState : State
{
    TheShade shade;

    public ShadeDriftState(TheShade _shade)
    {
        shade = _shade;
    }

    public override void Enter()
    {
        shade.agent.speed = shade.driftSpeed;
        GameEvents.OnSoundEmitted += OnSoundHeard;
    }

    public override void Execute()
    {
        // Wander slowly — just spin in place if no waypoint is set
        // In the scene you can assign a perimeter path; for now it drifts on the spot
        if (!shade.agent.pathPending && shade.agent.remainingDistance < 0.3f)
        {
            // Pick a random nearby point to drift toward
            Vector3 randomDir = Random.insideUnitSphere * 5f;
            randomDir += shade.transform.position;
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(randomDir, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                shade.agent.SetDestination(hit.position);
        }
    }

    public override void Exit()
    {
        GameEvents.OnSoundEmitted -= OnSoundHeard;
    }

    void OnSoundHeard(Vector3 soundPos, float intensity)
    {
        float range = shade.soundRadius * intensity;
        if (Vector3.Distance(shade.transform.position, soundPos) <= range)
        {
            shade.lastSoundPos = soundPos;
            shade.ChangeState(shade.alertState);
        }
    }

    public override string ToString() => "Drift";
}
