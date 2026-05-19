using UnityEngine;

public class BasketballBall : MonoBehaviour
{
    public Rigidbody rb;
    public Collider ballCollider;

    [Header("Ball State")]
    public BasketballAgent agentOwner;
    public PlayerBasketballController playerOwner;
    public BasketballAgent lastAgentShooter;
    public PlayerBasketballController lastPlayerShooter;
    public bool isHeld;

    [Header("Shot Info")]
    public Vector3 lastShotPosition;
    public bool hasLastShotPosition;
    public bool shotInAir;
    public float shotTimer;

    [Header("Dribble Settings")]
    public float holdForwardOffset = 0.8f;
    public float dribbleMinHeight = 0.35f;
    public float dribbleMaxHeight = 1.25f;
    public float dribbleSpeed = 6f;

    [Header("Release Settings")]
    public float releaseForwardOffset = 0.4f;

    private float dribbleTimer;

    public bool IsHeldByAgent => agentOwner != null;
    public bool IsHeldByPlayer => playerOwner != null;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (ballCollider == null)
            ballCollider = GetComponent<Collider>();
    }

    private void FixedUpdate()
    {
        if (!isHeld)
            return;

        if (agentOwner != null)
        {
            DribbleFollow(agentOwner.transform);
        }
        else if (playerOwner != null)
        {
            DribbleFollow(playerOwner.transform);
        }
    }

    private void DribbleFollow(Transform ownerTransform)
    {
        dribbleTimer += Time.fixedDeltaTime * dribbleSpeed;

        float bounce = Mathf.Abs(Mathf.Sin(dribbleTimer));
        float currentHeight = Mathf.Lerp(dribbleMinHeight, dribbleMaxHeight, bounce);

        Vector3 dribblePosition =
            ownerTransform.position +
            ownerTransform.forward * holdForwardOffset +
            Vector3.up * currentHeight;

        if (rb != null)
        {
            rb.MovePosition(dribblePosition);
        }
        else
        {
            transform.position = dribblePosition;
        }
    }

    public void PickUp(BasketballAgent newOwner)
    {
        agentOwner = newOwner;
        playerOwner = null;

        ResetShotState();
        BeginHeldState();
    }

    public void PickUp(PlayerBasketballController newOwner)
    {
        playerOwner = newOwner;
        agentOwner = null;

        ResetShotState();
        BeginHeldState();
    }

    public void StealToAgent(BasketballAgent newOwner)
    {
        if (newOwner == null)
            return;

        agentOwner = newOwner;
        playerOwner = null;

        ResetShotState();
        BeginHeldState();
    }

    public void StealToPlayer(PlayerBasketballController newOwner)
    {
        if (newOwner == null)
            return;

        playerOwner = newOwner;
        agentOwner = null;

        ResetShotState();
        BeginHeldState();
    }

    private void BeginHeldState()
    {
        isHeld = true;
        dribbleTimer = 0f;

        if (rb != null)
        {
            // Clear velocity BEFORE making the Rigidbody kinematic.
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.useGravity = false;
            rb.isKinematic = true;
        }

        if (ballCollider != null)
            ballCollider.enabled = false;
    }

    private void ResetShotState()
    {
        lastAgentShooter = null;
        lastPlayerShooter = null;

        hasLastShotPosition = false;
        lastShotPosition = Vector3.zero;

        shotInAir = false;
        shotTimer = 0f;
    }

    public void ShootFromAgent(Vector3 velocity)
    {
        lastAgentShooter = agentOwner;
        lastPlayerShooter = null;

        Transform shooterTransform = agentOwner != null ? agentOwner.transform : null;

        if (agentOwner != null)
        {
            lastShotPosition = agentOwner.transform.position;
            hasLastShotPosition = true;
        }

        shotInAir = true;
        shotTimer = 0f;

        agentOwner = null;
        playerOwner = null;
        isHeld = false;

        ReleaseBall(velocity, shooterTransform);
    }

    public void ShootFromPlayer(Vector3 velocity)
    {
        lastPlayerShooter = playerOwner;
        lastAgentShooter = null;

        Transform shooterTransform = playerOwner != null ? playerOwner.transform : null;

        if (playerOwner != null)
        {
            lastShotPosition = playerOwner.transform.position;
            hasLastShotPosition = true;
        }

        shotInAir = true;
        shotTimer = 0f;

        agentOwner = null;
        playerOwner = null;
        isHeld = false;

        ReleaseBall(velocity, shooterTransform);
    }

    private void ReleaseBall(Vector3 velocity, Transform shooterTransform)
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (shooterTransform != null)
        {
            transform.position += shooterTransform.forward * releaseForwardOffset;
        }

        if (ballCollider != null)
            ballCollider.enabled = true;

        if (rb != null)
        {
            rb.linearVelocity = velocity;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ResetBallToAgent(BasketballAgent owner)
    {
        if (owner == null)
            return;

        Vector3 holdPosition =
            owner.transform.position +
            owner.transform.forward * holdForwardOffset +
            Vector3.up * dribbleMaxHeight;

        ResetBall(holdPosition);
        PickUp(owner);
    }

    public void ResetBallToPlayer(PlayerBasketballController owner)
    {
        if (owner == null)
            return;

        Vector3 holdPosition =
            owner.transform.position +
            owner.transform.forward * holdForwardOffset +
            Vector3.up * dribbleMaxHeight;

        ResetBall(holdPosition);
        PickUp(owner);
    }
    public void ResetBall(Vector3 position)
    {
        agentOwner = null;
        playerOwner = null;

        ResetShotState();

        isHeld = false;
        dribbleTimer = 0f;

        if (ballCollider != null)
            ballCollider.enabled = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.position = position;
            rb.rotation = Quaternion.identity;
        }

        transform.position = position;
        transform.rotation = Quaternion.identity;

        if (rb != null)
        {
            rb.Sleep();
            rb.WakeUp();
        }
    }
}