using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class BasketballAgent : Agent
{
    [Header("References")]
    public BasketballEnvController envController;
    public BasketballBall ball;
    public PlayerBasketballController playerOpponent;
    public Transform shotTarget;

    [Header("Team")]
    public bool isRedTeam;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 160f;

    [Header("Smooth Movement")]
    public float acceleration = 12f;
    public float rotationSmoothSpeed = 8f;
    public float maxVelocity = 6f;

    [Header("Shooting")]
    public float shotArcHeight = 2.8f;
    public float jumpShotHeight = 1f;
    public float shotTargetHeightOffset = 0.6f;
    public float shotVelocityMultiplier = 1.08f;
    public float pickupLockoutAfterShot = 0.75f;

    [Header("Possession")]
    public float maxHoldTime = 6f;

    [Header("Reward Settings")]
    public float scoreReward = 2.0f;
    public float pickupReward = 0.25f;
    public float moveCloserToBallReward = 0.01f;
    public float shootReward = 0.05f;
    public float noBallShootPenalty = -0.03f;
    public float badShotPenalty = -0.05f;
    public float timePenalty = -0.001f;
    public float holdingPenalty = -0.005f;
    public float holdTooLongPenalty = -0.4f;
    public float defensiveReward = 0.005f;

    [Header("Realistic Shot Timing")]
    public float minimumHoldTimeBeforeShot = 2.5f;
    public float rushedShotPenalty = -1f;

    [Header("Contested Shot")]
    public float contestedShotDistance = 2.5f;
    public float contestedShotPenalty = -0.5f;

    [Header("Shot Accuracy")]
    public float shotRandomness = 0.35f;

    [Header("Scoring Strategy Rewards")]
    public float twoPointScoreBonus = 2.0f;

    [Header("Shot Selection Rewards")]
    public float twoPointZoneMinDistance = 7f;
    public float twoPointZoneMaxDistance = 10f;
    public float closeShotMinDistance = 3.5f;
    public float twoPointZoneReward = 0.02f;
    public float closeShotZoneReward = 0.006f;
    public float tooClosePenalty = -0.008f;
    public float facingHoopReward = 0.006f;
    public float twoPointShotReward = 0.25f;
    public float closeShotReward = 0.08f;
    public float badFacingShotPenalty = -0.15f;
    public float goodFacingShotReward = 0.1f;

    [Header("Stealing")]
    public float stealRange = 2.5f;
    public float stealCooldown = 1f;
    public float stealReward = 0.35f;
    public float failedStealPenalty = -0.04f;
    public float turnoverPenalty = -0.35f;
    public float minStealChance = 0.15f;
    public float maxStealChance = 0.85f;
    public float facingBonus = 0.2f;

    private Rigidbody rb;
    private float lastDistanceToBall;
    private float lastDistanceToHoop;
    private float holdTimer;
    private bool isGrounded = true;
    private float nextStealTime;
    private float nextAllowedPickupTime;

    public bool HasBall => ball != null && ball.agentOwner == this;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public override void OnEpisodeBegin()
    {
        holdTimer = 0f;
        isGrounded = true;
        nextStealTime = 0f;
        nextAllowedPickupTime = 0f;

        if (ball != null)
            lastDistanceToBall = Vector3.Distance(transform.position, ball.transform.position);

        if (shotTarget != null)
            lastDistanceToHoop = GetFlatDistanceToHoop();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(rb.linearVelocity);

        sensor.AddObservation(ball.transform.localPosition);
        sensor.AddObservation(ball.rb.linearVelocity);

        sensor.AddObservation(shotTarget.localPosition);
        sensor.AddObservation(playerOpponent.transform.localPosition);

        sensor.AddObservation((ball.transform.position - transform.position).normalized);
        sensor.AddObservation((shotTarget.position - transform.position).normalized);
        sensor.AddObservation((playerOpponent.transform.position - transform.position).normalized);

        sensor.AddObservation(HasBall ? 1f : 0f);
        sensor.AddObservation(playerOpponent.HasBall ? 1f : 0f);

        float distanceToBall = Vector3.Distance(transform.position, ball.transform.position);
        float distanceToHoop = GetFlatDistanceToHoop();
        float distanceToPlayer = Vector3.Distance(transform.position, playerOpponent.transform.position);

        sensor.AddObservation(distanceToBall);
        sensor.AddObservation(distanceToHoop);
        sensor.AddObservation(distanceToPlayer);

        Vector3 directionToHoop = shotTarget.position - transform.position;
        directionToHoop.y = 0f;

        if (directionToHoop.sqrMagnitude > 0.001f)
            directionToHoop.Normalize();

        float facingHoopDot = Vector3.Dot(transform.forward, directionToHoop);

        sensor.AddObservation(facingHoopDot);
        sensor.AddObservation(distanceToHoop >= twoPointZoneMinDistance && distanceToHoop <= twoPointZoneMaxDistance ? 1f : 0f);
        sensor.AddObservation(distanceToHoop >= closeShotMinDistance && distanceToHoop < twoPointZoneMinDistance ? 1f : 0f);
        sensor.AddObservation(distanceToHoop < closeShotMinDistance ? 1f : 0f);
        sensor.AddObservation(holdTimer / Mathf.Max(0.01f, maxHoldTime));
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (!HasBall)
        {
            actionMask.SetActionEnabled(0, 1, false);
        }

        if (playerOpponent == null || !playerOpponent.HasBall)
        {
            actionMask.SetActionEnabled(0, 2, false);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float forward = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float strafe = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float rotate = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);

        int action = actions.DiscreteActions[0];

        MoveAgent(forward, strafe, rotate);

        if (action == 1)
        {
            TryShoot();
        }
        else if (action == 2)
        {
            TrySteal();
        }

        HandlePossessionTimer();
        GiveSmallRewards();
    }

    private void MoveAgent(float forward, float strafe, float rotate)
    {
        Vector3 inputDirection =
            transform.forward * forward +
            transform.right * strafe;

        if (inputDirection.sqrMagnitude > 1f)
            inputDirection.Normalize();

        Vector3 targetVelocity = inputDirection * moveSpeed;

        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        Vector3 smoothedVelocity = Vector3.Lerp(
            horizontalVelocity,
            targetVelocity,
            acceleration * Time.fixedDeltaTime
        );

        smoothedVelocity = Vector3.ClampMagnitude(smoothedVelocity, maxVelocity);

        rb.linearVelocity = new Vector3(
            smoothedVelocity.x,
            rb.linearVelocity.y,
            smoothedVelocity.z
        );

        float targetYRotation =
            transform.eulerAngles.y + rotate * rotateSpeed * Time.fixedDeltaTime;

        Quaternion targetRotation = Quaternion.Euler(0f, targetYRotation, 0f);

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSmoothSpeed * Time.fixedDeltaTime
            )
        );

        rb.angularVelocity = Vector3.zero;
    }

    private void TryShoot()
    {
        if (!HasBall)
        {
            AddReward(noBallShootPenalty);
            return;
        }

        if (shotTarget == null)
        {
            Debug.LogWarning("ML Agent has no Shot Target assigned.");
            return;
        }

        float distanceToHoop = GetFlatDistanceToHoop();
        float facingDot = GetFacingHoopDot();

        if (facingDot < 0.5f)
        {
            AddReward(badFacingShotPenalty);
        }
        else if (facingDot > 0.8f)
        {
            AddReward(goodFacingShotReward);
        }

        if (holdTimer < minimumHoldTimeBeforeShot)
        {
            AddReward(rushedShotPenalty);
        }

        float defenderDistance = Vector3.Distance(transform.position, playerOpponent.transform.position);

        if (defenderDistance < contestedShotDistance)
        {
            AddReward(contestedShotPenalty);
        }

        PerformShot();

        AddReward(shootReward);

        if (distanceToHoop >= twoPointZoneMinDistance &&
            distanceToHoop <= twoPointZoneMaxDistance)
        {
            AddReward(twoPointShotReward);
        }
        else if (distanceToHoop >= closeShotMinDistance &&
                 distanceToHoop < twoPointZoneMinDistance)
        {
            AddReward(closeShotReward);
        }
        else
        {
            AddReward(badShotPenalty);
        }
    }

    private void PerformShot()
    {
        if (!HasBall || shotTarget == null)
            return;

        Vector3 targetPosition =
            shotTarget.position + Vector3.up * shotTargetHeightOffset;

        Vector3 shotVelocity = CalculateShotVelocity(
            ball.transform.position,
            targetPosition,
            shotArcHeight
        );

        shotVelocity *= shotVelocityMultiplier;

        ball.ShootFromAgent(shotVelocity);

        holdTimer = 0f;
        nextAllowedPickupTime = Time.time + pickupLockoutAfterShot;

        if (isGrounded)
        {
            JumpToHeight(jumpShotHeight);
        }
    }

    private void TrySteal()
    {
        if (Time.time < nextStealTime)
            return;

        nextStealTime = Time.time + stealCooldown;

        if (ball == null || playerOpponent == null)
            return;

        if (!playerOpponent.HasBall)
        {
            AddReward(failedStealPenalty);
            return;
        }

        float distanceToPlayer =
            Vector3.Distance(transform.position, playerOpponent.transform.position);

        if (distanceToPlayer > stealRange)
        {
            AddReward(failedStealPenalty);
            return;
        }

        float distanceFactor = 1f - Mathf.Clamp01(distanceToPlayer / stealRange);

        Vector3 directionToPlayer =
            (playerOpponent.transform.position - transform.position).normalized;

        float facingDot =
            Vector3.Dot(transform.forward, directionToPlayer);

        float facingFactor =
            Mathf.InverseLerp(0.2f, 1f, facingDot);

        float stealChance =
            Mathf.Lerp(minStealChance, maxStealChance, distanceFactor);

        stealChance += facingFactor * facingBonus;
        stealChance = Mathf.Clamp01(stealChance);

        float roll = Random.value;

        if (roll <= stealChance)
        {
            ball.StealToAgent(this);

            AddReward(stealReward);

            if (envController != null)
                envController.ResetShotClockForNewPossession();

            Debug.Log("AI stole the ball! Chance: " + stealChance);
        }
        else
        {
            AddReward(failedStealPenalty);
            Debug.Log("AI steal failed. Chance: " + stealChance);
        }
    }

    private void ForceShoot()
    {
        if (!HasBall)
            return;

        AddReward(holdTooLongPenalty);
        PerformShot();
    }

    private void HandlePossessionTimer()
    {
        if (HasBall)
        {
            holdTimer += Time.fixedDeltaTime;

            AddReward(holdingPenalty * Time.fixedDeltaTime);

            if (holdTimer >= maxHoldTime)
            {
                ForceShoot();
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    private void GiveSmallRewards()
    {
        AddReward(timePenalty);

        if (ball == null || shotTarget == null || playerOpponent == null)
            return;

        float currentDistanceToBall =
            Vector3.Distance(transform.position, ball.transform.position);

        if (!HasBall && ball.playerOwner == null)
        {
            if (currentDistanceToBall < lastDistanceToBall)
            {
                AddReward(moveCloserToBallReward);
            }
        }

        lastDistanceToBall = currentDistanceToBall;

        if (HasBall)
        {
            float distanceToHoop = GetFlatDistanceToHoop();
            float facingDot = GetFacingHoopDot();

            if (facingDot > 0.7f)
            {
                AddReward(facingHoopReward * Time.fixedDeltaTime);
            }

            if (distanceToHoop >= twoPointZoneMinDistance &&
                distanceToHoop <= twoPointZoneMaxDistance)
            {
                AddReward(twoPointZoneReward * Time.fixedDeltaTime);
            }
            else if (distanceToHoop >= closeShotMinDistance &&
                     distanceToHoop < twoPointZoneMinDistance)
            {
                AddReward(closeShotZoneReward * Time.fixedDeltaTime);
            }
            else if (distanceToHoop < closeShotMinDistance)
            {
                AddReward(tooClosePenalty * Time.fixedDeltaTime);
            }

            lastDistanceToHoop = distanceToHoop;
        }

        if (playerOpponent.HasBall)
        {
            float distanceToPlayer =
                Vector3.Distance(transform.position, playerOpponent.transform.position);

            if (distanceToPlayer < 3f)
            {
                AddReward(defensiveReward);
            }
        }
    }

    private float GetFacingHoopDot()
    {
        if (shotTarget == null)
            return 0f;

        Vector3 directionToHoop =
            shotTarget.position - transform.position;

        directionToHoop.y = 0f;

        if (directionToHoop.sqrMagnitude <= 0.001f)
            return 0f;

        directionToHoop.Normalize();

        return Vector3.Dot(transform.forward, directionToHoop);
    }

    private float GetFlatDistanceToHoop()
    {
        if (shotTarget == null)
            return 0f;

        Vector3 agentPosition = transform.position;
        Vector3 hoopPosition = shotTarget.position;

        agentPosition.y = 0f;
        hoopPosition.y = 0f;

        return Vector3.Distance(agentPosition, hoopPosition);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            if (Time.time < nextAllowedPickupTime)
                return;

            if (ball != null && !ball.isHeld)
            {
                ball.PickUp(this);
                holdTimer = 0f;

                if (envController != null)
                    envController.ResetShotClockForNewPossession();

                AddReward(pickupReward);
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

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        continuousActions[0] = Input.GetAxisRaw("Vertical");
        continuousActions[1] = Input.GetAxisRaw("Horizontal");

        if (Input.GetKey(KeyCode.E))
            continuousActions[2] = 1f;
        else if (Input.GetKey(KeyCode.Q))
            continuousActions[2] = -1f;
        else
            continuousActions[2] = 0f;

        if (Input.GetKey(KeyCode.Space))
        {
            discreteActions[0] = 1;
        }
        else if (Input.GetKey(KeyCode.F))
        {
            discreteActions[0] = 2;
        }
        else
        {
            discreteActions[0] = 0;
        }
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
}