using UnityEngine;

// Handles player movement, torch toggle, and stamina
// Fires sound events based on movement speed so the Shade can react
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed  = 4f;
    public float sneakSpeed = 1.8f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 15f;  // per second while Shade is in contact
    public float staminaRegenRate = 8f;

    [Header("Footstep Sounds")]
    public float footstepInterval = 0.4f;  // seconds between footstep sounds
    public float walkSoundIntensity  = 0.6f;
    public float sneakSoundIntensity = 0.15f;

    // Runtime
    float currentStamina;
    bool torchOn = false;
    bool shadeDraining = false;

    float footstepTimer = 0f;
    CharacterController controller;
    Vector3 velocity;
    float gravity = -9.81f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;
    }

    void OnEnable()
    {
        GameEvents.OnShadeContactPlayer += OnShadeContact;
    }

    void OnDisable()
    {
        GameEvents.OnShadeContactPlayer -= OnShadeContact;
    }

    void Update()
    {
        HandleMovement();
        HandleTorch();
        HandleStamina();
    }

    void HandleMovement()
    {
        bool sneaking = Input.GetKey(KeyCode.LeftShift);
        float speed = sneaking ? sneakSpeed : walkSpeed;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 move = new Vector3(h, 0f, v).normalized;

        // Gravity
        if (controller.isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        controller.Move((move * speed + velocity) * Time.deltaTime);

        // Face the direction of movement
        if (move.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 15f * Time.deltaTime);

        // Emit footstep sounds while moving
        if (move.sqrMagnitude > 0.01f)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                footstepTimer = 0f;
                float intensity = sneaking ? sneakSoundIntensity : walkSoundIntensity;
                SoundEmitter.Emit(transform.position, intensity);
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    void HandleTorch()
    {
        // Toggle torch with F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            torchOn = !torchOn;
            GameEvents.OnTorchToggled?.Invoke(torchOn);
            Debug.Log($"[Player] Torch: {(torchOn ? "ON" : "OFF")}");
        }
    }

    void HandleStamina()
    {
        if (shadeDraining)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina  = Mathf.Max(0f, currentStamina);
            shadeDraining   = false; // reset each frame — Hunt state fires every frame on contact
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina  = Mathf.Min(maxStamina, currentStamina);
        }
    }

    // Called when Shade makes contact
    void OnShadeContact()
    {
        shadeDraining = true;
    }

    // Called by UI to display stamina (0 to 1)
    public float GetStaminaPercent() => currentStamina / maxStamina;
    public bool IsTorchOn() => torchOn;
}
