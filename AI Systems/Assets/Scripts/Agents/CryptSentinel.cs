using UnityEngine;
using UnityEngine.AI;
using TMPro;

// Crypt Sentinel — heavy undead guardian
// Owns the FSM and switches between: Patrol, Investigate, Combat, Staggered, Rallying, Return
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(VisionCone))]
public class CryptSentinel : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public Transform[] patrolWaypoints;
    public TextMeshProUGUI stateLabel;

    [Header("Movement")]
    public float patrolSpeed = 1.8f;
    public float chargeSpeed = 3.2f;

    [Header("Combat")]
    public float attackRange = 1.5f;
    public float staggerDuration = 1.5f;
    public float maxHP = 100f;
    [Range(0f, 1f)] public float rallyHPThreshold = 0.35f;

    [Header("Detection")]
    public float memoryDuration = 4f;   // seconds before giving up on last known position
    public float hearingRange = 6f;     // base hearing range at full intensity

    [Header("Rallying Cry")]
    public float rallyRadius = 10f;

    // Runtime values other states can read/write
    [HideInInspector] public float currentHP;
    [HideInInspector] public Vector3 lastKnownPlayerPos;
    [HideInInspector] public int patrolIndex = 0;

    // Component references states will need
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public VisionCone vision;

    // All six states
    [HideInInspector] public SentinelPatrolState    patrolState;
    [HideInInspector] public SentinelInvestigateState investigateState;
    [HideInInspector] public SentinelCombatState    combatState;
    [HideInInspector] public SentinelStaggeredState  staggeredState;
    [HideInInspector] public SentinelRallyingState   rallyingState;
    [HideInInspector] public SentinelReturnState     returnState;

    State currentState;

    void Awake()
    {
        agent  = GetComponent<NavMeshAgent>();
        vision = GetComponent<VisionCone>();

        currentHP = maxHP;

        // Create all states once and reuse them
        patrolState      = new SentinelPatrolState(this);
        investigateState = new SentinelInvestigateState(this);
        combatState      = new SentinelCombatState(this);
        staggeredState   = new SentinelStaggeredState(this);
        rallyingState    = new SentinelRallyingState(this);
        returnState      = new SentinelReturnState(this);
    }

    void Start()
    {
        ChangeState(patrolState);
    }

    void Update()
    {
        if (currentState != null)
        {
            currentState.Execute();

            // Show current state name on screen
            if (stateLabel != null)
                stateLabel.text = $"Sentinel: {currentState}";
        }
    }

    public void ChangeState(State newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    // Called by LevelManager when OnPlayerHitSentinel fires
    public void OnHitByPlayer()
    {
        if (currentState is SentinelCombatState)
            ChangeState(staggeredState);
    }

    // Called by LevelManager when OnSentinelLowHP fires
    public void OnLowHP()
    {
        if (currentState is SentinelCombatState)
            ChangeState(rallyingState);
    }

    // Called by LevelManager when Shade shares a confirmed player position
    public void OnShadeSharedPosition(Vector3 pos)
    {
        lastKnownPlayerPos = pos;
        if (currentState is SentinelPatrolState)
            ChangeState(investigateState);
    }

    // Apply damage and fire low HP event when threshold is crossed
    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        if (currentHP / maxHP <= rallyHPThreshold)
            GameEvents.OnSentinelLowHP?.Invoke();
        if (currentHP <= 0f)
            gameObject.SetActive(false);
    }
}
