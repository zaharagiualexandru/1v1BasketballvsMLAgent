using UnityEngine;

public class PlayerBasketballController : MonoBehaviour
{
    [Header("References")]
    public BasketballBall ball;
    public Transform shotTarget;
    public BasketballAgent mlAgentOpponent;
    public BasketballEnvController envController;

    [Header("Camera")]
    public Transform cameraPivot;
    public Camera playerCamera;
    public float mouseSensitivity = 2f;
    public float minLookAngle = -35f;
    public float maxLookAngle = 60f;
    public float mouseDeadzone = 0.01f;

    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Shooting")]
    public float shotArcHeight = 2.2f;
    public float jumpShotHeight = 1f;

    [Header("Stealing")]
    public float stealRange = 2.5f;
    public float stealCooldown = 1f;
    public float minStealChance = 0.15f;
    public float maxStealChance = 0.85f;
    public float facingBonus = 0.2f;

    private Rigidbody rb;

    private bool isGrounded = true;

    private float yaw;
    private float pitch;

    private float moveForward;
    private float moveStrafe;

    private float nextStealTime;

    public bool HasBall => ball != null && ball.playerOwner == this;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        yaw = transform.eulerAngles.y;
        pitch = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        ReadMouseLookInput();
        ReadMovementInput();

        if (Input.GetMouseButtonDown(0))
        {
            TryShoot();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            TrySteal();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursor();
        }
    }

    private void FixedUpdate()
    {
        ApplyRotation();
        MovePlayer();

        rb.angularVelocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        ApplyCameraPitch();
    }

    private void ReadMouseLookInput()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        if (Mathf.Abs(mouseX) < mouseDeadzone)
            mouseX = 0f;

        if (Mathf.Abs(mouseY) < mouseDeadzone)
            mouseY = 0f;

        yaw += mouseX * mouseSensitivity;
        pitch -= mouseY * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, minLookAngle, maxLookAngle);
    }

    private void ReadMovementInput()
    {
        moveForward = Input.GetAxisRaw("Vertical");
        moveStrafe = Input.GetAxisRaw("Horizontal");
    }

    private void ApplyRotation()
    {
        Quaternion targetRotation = Quaternion.Euler(0f, yaw, 0f);
        rb.MoveRotation(targetRotation);
    }

    private void ApplyCameraPitch()
    {
        if (cameraPivot == null)
            return;

        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void MovePlayer()
    {
        Vector3 moveDirection =
            transform.forward * moveForward +
            transform.right * moveStrafe;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        Vector3 targetPosition =
            rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(targetPosition);
    }

    private void TryShoot()
    {
        if (!HasBall)
            return;

        if (shotTarget == null)
        {
            Debug.LogWarning("Player has no Shot Target assigned.");
            return;
        }

        if (isGrounded)
        {
            JumpToHeight(jumpShotHeight);
        }

        Vector3 shotVelocity = CalculateShotVelocity(
            ball.transform.position,
            shotTarget.position,
            shotArcHeight
        );

        ball.ShootFromPlayer(shotVelocity);
    }

    private void TrySteal()
    {
        if (Time.time < nextStealTime)
            return;

        nextStealTime = Time.time + stealCooldown;

        if (ball == null || mlAgentOpponent == null)
            return;

        if (!mlAgentOpponent.HasBall)
            return;

        float distanceToOpponent =
            Vector3.Distance(transform.position, mlAgentOpponent.transform.position);

        if (distanceToOpponent > stealRange)
        {
            Debug.Log("Player steal failed: too far away.");
            return;
        }

        float distanceFactor = 1f - Mathf.Clamp01(distanceToOpponent / stealRange);

        Vector3 directionToOpponent =
            (mlAgentOpponent.transform.position - transform.position).normalized;

        float facingDot =
            Vector3.Dot(transform.forward, directionToOpponent);

        float facingFactor =
            Mathf.InverseLerp(0.2f, 1f, facingDot);

        float stealChance =
            Mathf.Lerp(minStealChance, maxStealChance, distanceFactor);

        stealChance += facingFactor * facingBonus;
        stealChance = Mathf.Clamp01(stealChance);

        float roll = Random.value;

        if (roll <= stealChance)
        {
            ball.StealToPlayer(this);

            mlAgentOpponent.AddReward(mlAgentOpponent.turnoverPenalty);

            if (envController != null)
                envController.ResetShotClockForNewPossession();

            Debug.Log("Player stole the ball! Chance: " + stealChance);
        }
        else
        {
            Debug.Log("Player steal failed. Chance: " + stealChance);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            if (ball != null && !ball.isHeld)
            {
                PickUpBallSafely();
            }
        }

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    private void PickUpBallSafely()
    {
        ball.PickUp(this);

        if (envController != null)
            envController.ResetShotClockForNewPossession();

        Vector3 currentVelocity = rb.linearVelocity;
        currentVelocity.x = Mathf.Clamp(currentVelocity.x, -moveSpeed, moveSpeed);
        currentVelocity.z = Mathf.Clamp(currentVelocity.z, -moveSpeed, moveSpeed);
        rb.linearVelocity = currentVelocity;

        rb.angularVelocity = Vector3.zero;
    }

    private void JumpToHeight(float jumpHeight)
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
        rb.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
    }

    private Vector3 CalculateShotVelocity(Vector3 startPosition, Vector3 targetPosition, float arcHeight)
    {
        Vector3 direction = targetPosition - startPosition;
        Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z);

        float verticalDifference = direction.y;
        float gravity = Mathf.Abs(Physics.gravity.y);

        float height = Mathf.Max(arcHeight, verticalDifference + arcHeight);

        float timeUp = Mathf.Sqrt(2f * height / gravity);
        float timeDown = Mathf.Sqrt(2f * Mathf.Max(0.1f, height - verticalDifference) / gravity);
        float totalTime = timeUp + timeDown;

        Vector3 horizontalVelocity = horizontalDirection / totalTime;
        float verticalVelocity = Mathf.Sqrt(2f * gravity * height);

        return horizontalVelocity + Vector3.up * verticalVelocity;
    }

    private void ToggleCursor()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}