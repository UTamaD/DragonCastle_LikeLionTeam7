using UnityEngine;

public class PlayerMovement : MonoBehaviour, IDamageable_th, IKnockbackable, IDebuffable
{
    public float moveSpeed = 5f;
    public float turnSpeed = 100f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    public float HP = 100;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private Vector3 knockbackVelocity;
    private float knockbackTimer;

    private float slowDebuffIntensity = 1f;
    private float slowDebuffTimer;
    private bool isStunned;
    private float stunDebuffTimer;
    private float dotDamage;
    private float dotTimer;
    private float defenseReduction;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        ApplyMovement();
        ApplyGravity();
        ApplyKnockback();
        ApplyDebuffs();
    }

    void ApplyMovement()
    {
        if (!isStunned)
        {
            float moveDirection = Input.GetAxis("Vertical");
            float turnDirection = Input.GetAxis("Horizontal");

            Vector3 move = transform.forward * moveDirection;
            controller.Move(move * moveSpeed * slowDebuffIntensity * Time.deltaTime);

            transform.Rotate(0, turnDirection * turnSpeed * Time.deltaTime, 0);

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void ApplyKnockback()
    {
        if (knockbackTimer > 0)
        {
            controller.Move(knockbackVelocity * Time.deltaTime);
            knockbackTimer -= Time.deltaTime;
        }
    }

    void ApplyDebuffs()
    {
        if (slowDebuffTimer > 0)
        {
            slowDebuffTimer -= Time.deltaTime;
            if (slowDebuffTimer <= 0)
            {
                slowDebuffIntensity = 1f;
            }
        }

        if (stunDebuffTimer > 0)
        {
            stunDebuffTimer -= Time.deltaTime;
            if (stunDebuffTimer <= 0)
            {
                isStunned = false;
            }
        }

        if (dotTimer > 0)
        {
            dotTimer -= Time.deltaTime;
            HP -= dotDamage * Time.deltaTime;
            if (dotTimer <= 0)
            {
                dotDamage = 0;
            }
        }
    }

    public void TakeDamage(float amount, DamageField.DamageType damageType)
    {
        float finalDamage = amount * (1 - defenseReduction);
        HP -= finalDamage;
        Debug.Log($"Player took {finalDamage} damage of type {damageType}. Current HP: {HP}");
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        knockbackVelocity = direction * force;
        knockbackTimer = duration;
    }

    public void ApplyDebuff(string debuffType, float duration, float intensity)
    {
        switch (debuffType)
        {
            case "Slow":
                slowDebuffIntensity = 1 - intensity;
                slowDebuffTimer = duration;
                break;
            case "Stun":
                isStunned = true;
                stunDebuffTimer = duration;
                break;
            case "DoT":
                dotDamage = intensity;
                dotTimer = duration;
                break;
            case "DefenseReduction":
                defenseReduction = intensity;
                Invoke(nameof(ResetDefenseReduction), duration);
                break;
        }
        Debug.Log($"Applied {debuffType} debuff for {duration} seconds with intensity {intensity}");
    }

    void ResetDefenseReduction()
    {
        defenseReduction = 0;
    }
}