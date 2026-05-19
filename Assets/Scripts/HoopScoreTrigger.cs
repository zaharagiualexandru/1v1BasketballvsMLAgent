using UnityEngine;

public class HoopScoreTrigger : MonoBehaviour
{
    public BasketballEnvController envController;
    public BackboardFeedback backboardFeedback;
    public NetReaction netReaction;

    [Header("Scoring Rules")]
    public bool requireDownwardMovement = true;
    public float minimumDownwardSpeed = -0.1f;

    private bool alreadyScored;

    private void OnTriggerEnter(Collider other)
    {
        BasketballBall ball = other.GetComponent<BasketballBall>();

        if (ball == null)
            return;

        if (alreadyScored)
            return;

        if (ball.lastAgentShooter == null && ball.lastPlayerShooter == null)
            return;

        if (requireDownwardMovement && ball.rb.linearVelocity.y > minimumDownwardSpeed)
            return;

        alreadyScored = true;

        ball.shotInAir = false;
        ball.shotTimer = 0f;

        if (backboardFeedback != null)
            backboardFeedback.ShowScore();

        if (netReaction != null)
            netReaction.PlayNetReaction();

        if (envController != null)
            envController.Score(ball.lastAgentShooter, ball.lastPlayerShooter);

        Invoke(nameof(ResetScoreTrigger), 0.5f);
    }

    private void ResetScoreTrigger()
    {
        alreadyScored = false;
    }
}