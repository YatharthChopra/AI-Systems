using UnityEngine;
using UnityEngine.AI;
using TMPro;

// The Shade — spectral stalker that hunts by sound and retreats from torchlight
// Owns the FSM and switches between: Drift, Alert, Stalk, Hunt, Retreat, Rallied
[RequireComponent(typeof(NavMeshAgent))]
public class TheShade : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public TextMeshProUGUI stateLabel;

    [Header("Movement")]
    public float driftSpeed  = 2.5f;
    public float huntSpeed   = 4.5f;

    [Header("Sensors")]
    public float soundRadius = 8f;     // reacts to sounds within this range
    public float lightRadius = 5f;     // detects a lit torch within this range
    public float huntRange   = 3f;     // enters Hunt when player is this close

    [Header("Timers")]
    public float soundFadeTime = 3f;   // how long Alert lasts before returning to Drift
    public float retreatRadius = 10f;  // how far to flee before considering itself safe

    // Runtime values
    [HideInInspector] public Vector3 lastSoundPos;
    [HideInInspector] public bool torchIsLit = false;

    // Component references
    [HideInInspector] public NavMeshAgent agent;

    // All six states
    [HideInInspector] public ShadeDriftState    driftState;
    [HideInInspector] public ShadeAlertState    alertState;
    [HideInInspector] public ShadeStalkState    stalkState;
    [HideInInspector] public ShadeHuntState     huntState;
    [HideInInspector] public ShadeRetreatState  retreatState;
    [HideInInspector] public ShadeRalliedState  ralliedState;

    State currentState;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        driftState   = new ShadeDriftState(this);
        alertState   = new ShadeAlertState(this);
        stalkState   = new ShadeStalkState(this);
        huntState    = new ShadeHuntState(this);
        retreatState = new ShadeRetreatState(this);
        ralliedState = new ShadeRalliedState(this);
    }

    void Start()
    {
        ChangeState(driftState);
    }

    void Update()
    {
        if (currentState != null)
        {
            currentState.Execute();

            if (stateLabel != null)
                stateLabel.text = $"Shade: {currentState}";
        }
    }

    public void ChangeState(State newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    // Called by LevelManager when OnSentinelRallyingCry fires
    public void OnRallyingCry(Vector3 sentinelPos)
    {
        // Only respond if within rally radius
        if (Vector3.Distance(transform.position, sentinelPos) <= 10f)
            ChangeState(ralliedState);
    }

    // Called by LevelManager when OnTorchToggled fires
    public void OnTorchToggled(bool isLit)
    {
        torchIsLit = isLit;

        // If torch just turned on and Shade is close, immediately retreat
        if (isLit && Vector3.Distance(transform.position, playerTransform.position) <= lightRadius)
            ChangeState(retreatState);
    }
}
