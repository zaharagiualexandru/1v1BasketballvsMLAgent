using UnityEngine;

public class MissTrigger : MonoBehaviour
{
    public BackboardFeedback backboardFeedback;

    private float lastMissTime;
    public float missCooldown = 1f;

    private void OnTriggerEnter(Collider other)
    {
        BasketballBall ball = other.GetComponent<BasketballBall>();

        if (ball == null)
            return;

        if (Time.time < lastMissTime + missCooldown)
            return;

        if (ball.lastAgentShooter == null && ball.lastPlayerShooter == null)
            return;

        lastMissTime = Time.time;

        if (backboardFeedback != null)
            backboardFeedback.ShowMiss();
    }
}