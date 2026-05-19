using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public enum PossessionTarget
{
    Loose,
    Agent,
    Player
}

public class BasketballEnvController : MonoBehaviour
{
    [Header("Characters")]
    public BasketballAgent mlAgent;
    public PlayerBasketballController player;

    [Header("Ball")]
    public BasketballBall ball;

    [Header("Spawn Points")]
    public Transform agentSpawn;
    public Transform playerSpawn;
    public Transform ballSpawn;

    [Header("Possession Reset")]
    public Transform[] possessionStarts;
    public float defenderDistance = 3f;

    [Header("Scoring")]
    public Transform shotTarget;
    public int playerScore;
    public int agentScore;
    public int pointsToWin = 21;
    public float twoPointDistance = 7f;

    [Header("Shot Clock")]
    public float shotClockDuration = 12f;
    public TMP_Text shotClockText;

    [Header("Training Reset Rules")]
    public float maxEpisodeTime = 45f;
    public float ballFallYLimit = -5f;
    public float maxLooseBallTime = 8f;
    public float maxShotAirTime = 6f;

    [Header("UI")]
    public TMP_Text playerScoreText;
    public TMP_Text agentScoreText;
    public TMP_Text possessionText;
    public TMP_Text winText;

    [Header("End Game UI")]
    public GameObject winPanel;
    public string mainMenuSceneName = "MainMenu";

    private float timer;
    private float shotClockTimer;
    private float looseBallTimer;

    private bool shotClockRunning;
    private bool gameOver;
    private bool isResetting;

    private void Start()
    {
        playerScore = 0;
        agentScore = 0;
        gameOver = false;
        isResetting = false;

        if (winText != null)
            winText.gameObject.SetActive(false);

        ResetEnvironment();
        UpdateUI();
    }

    private void FixedUpdate()
    {
        if (gameOver || isResetting)
            return;

        timer += Time.fixedDeltaTime;

        HandleShotClock();
        HandleLooseBallTimer();
        HandleShotAirTimer();
        CheckEpisodeTimeout();
        CheckBallFellOut();

        UpdateUI();
    }

    private void CheckEpisodeTimeout()
    {
        if (timer < maxEpisodeTime)
            return;

        Debug.LogWarning(gameObject.name + " episode timed out.");

        EndTrainingEpisodeWithPossession(-0.2f, "Episode timeout", PossessionTarget.Loose);
    }

    private void CheckBallFellOut()
    {
        if (ball == null)
            return;

        if (ball.transform.position.y > ballFallYLimit)
            return;

        Debug.LogWarning(gameObject.name + " ball fell out.");

        EndTrainingEpisodeWithPossession(-0.1f, "Ball fell below Y limit", PossessionTarget.Loose);
    }

    private void HandleLooseBallTimer()
    {
        if (ball == null)
            return;

        bool ballHasOwner =
            ball.agentOwner != null ||
            ball.playerOwner != null;

        if (ballHasOwner)
        {
            looseBallTimer = 0f;
            return;
        }

        if (ball.shotInAir)
            return;

        looseBallTimer += Time.fixedDeltaTime;

        if (looseBallTimer >= maxLooseBallTime)
        {
            Debug.LogWarning(gameObject.name + " loose ball timeout.");

            EndTrainingEpisodeWithPossession(-0.05f, "Loose ball timeout", PossessionTarget.Loose);
        }
    }

    private void HandleShotAirTimer()
    {
        if (ball == null)
            return;

        if (!ball.shotInAir)
            return;

        ball.shotTimer += Time.fixedDeltaTime;

        if (ball.shotTimer >= maxShotAirTime)
        {
            Debug.LogWarning(gameObject.name + " shot missed / shot air timeout.");

            if (ball.lastAgentShooter != null)
            {
                EndTrainingEpisodeWithPossession(
                    -0.15f,
                    "Agent missed, player gets possession",
                    PossessionTarget.Player
                );
            }
            else if (ball.lastPlayerShooter != null)
            {
                EndTrainingEpisodeWithPossession(
                    0.15f,
                    "Player missed, agent gets possession",
                    PossessionTarget.Agent
                );
            }
            else
            {
                EndTrainingEpisodeWithPossession(
                    -0.05f,
                    "Missed shot with unknown shooter",
                    PossessionTarget.Loose
                );
            }
        }
    }

    private void HandleShotClock()
    {
        if (ball == null)
            return;

        bool hasPossession =
            ball.agentOwner != null ||
            ball.playerOwner != null;

        if (!hasPossession)
        {
            shotClockRunning = false;
            return;
        }

        if (!shotClockRunning)
        {
            ResetShotClockForNewPossession();
        }

        shotClockTimer -= Time.fixedDeltaTime;

        if (shotClockTimer <= 0f)
        {
            ShotClockViolation();
        }
    }

    public void ResetShotClockForNewPossession()
    {
        shotClockRunning = true;
        shotClockTimer = shotClockDuration;
    }

    private void ResetShotClock()
    {
        shotClockRunning = false;
        shotClockTimer = shotClockDuration;
    }

    private void ShotClockViolation()
    {
        Debug.LogWarning(gameObject.name + " shot clock violation.");

        if (ball != null && ball.agentOwner != null)
        {
            EndTrainingEpisodeWithPossession(
                -0.4f,
                "Agent shot clock violation, player gets possession",
                PossessionTarget.Player
            );
        }
        else if (ball != null && ball.playerOwner != null)
        {
            EndTrainingEpisodeWithPossession(
                0.2f,
                "Player shot clock violation, agent gets possession",
                PossessionTarget.Agent
            );
        }
        else
        {
            EndTrainingEpisodeWithPossession(
                -0.05f,
                "Shot clock violation with no owner",
                PossessionTarget.Loose
            );
        }
    }

    public void Score(BasketballAgent scoringAgent, PlayerBasketballController scoringPlayer)
    {
        if (gameOver || isResetting)
            return;

        int points = CalculateShotPoints(scoringAgent, scoringPlayer);

        if (scoringAgent != null)
        {
            agentScore += points;

            Debug.Log(gameObject.name + " Agent scored " + points + " point(s).");

            scoringAgent.AddReward(scoringAgent.scoreReward * points);

            if (points == 2)
                scoringAgent.AddReward(scoringAgent.twoPointScoreBonus);

            if (agentScore >= pointsToWin)
            {
                EndGame("Agent Wins!");
                return;
            }

            EndTrainingEpisodeWithPossession(
                0f,
                "Agent scored and keeps possession",
                PossessionTarget.Agent
            );

            return;
        }

        if (scoringPlayer != null)
        {
            playerScore += points;

            Debug.Log(gameObject.name + " Player scored " + points + " point(s).");

            if (mlAgent != null)
                mlAgent.AddReward(-1.0f);

            if (playerScore >= pointsToWin)
            {
                EndGame("Player Wins!");
                return;
            }

            EndTrainingEpisodeWithPossession(
                0f,
                "Player scored and keeps possession",
                PossessionTarget.Player
            );

            return;
        }

        EndTrainingEpisodeWithPossession(0f, "Score happened", PossessionTarget.Loose);
    }

    private void EndTrainingEpisodeWithPossession(float agentReward, string reason, PossessionTarget nextPossession)
    {
        if (isResetting)
            return;

        isResetting = true;

        Debug.Log(gameObject.name + " ending episode. Reason: " + reason);

        if (mlAgent != null)
        {
            if (agentReward != 0f)
                mlAgent.AddReward(agentReward);

            mlAgent.EndEpisode();
        }
        else
        {
            Debug.LogWarning(gameObject.name + " cannot end episode because ML Agent reference is missing.");
        }

        ResetEnvironmentWithPossession(nextPossession);

        isResetting = false;
    }

    private int CalculateShotPoints(BasketballAgent scoringAgent, PlayerBasketballController scoringPlayer)
    {
        if (shotTarget == null)
            return 1;

        Vector3 shooterPosition;

        if (ball != null && ball.hasLastShotPosition)
        {
            shooterPosition = ball.lastShotPosition;
        }
        else if (scoringAgent != null)
        {
            shooterPosition = scoringAgent.transform.position;
        }
        else if (scoringPlayer != null)
        {
            shooterPosition = scoringPlayer.transform.position;
        }
        else
        {
            return 1;
        }

        Vector3 flatShooter = shooterPosition;
        Vector3 flatHoop = shotTarget.position;

        flatShooter.y = 0f;
        flatHoop.y = 0f;

        float distance = Vector3.Distance(flatShooter, flatHoop);

        return distance >= twoPointDistance ? 2 : 1;
    }

    private void EndGame(string message)
    {
        gameOver = true;

        if (mlAgent != null)
            mlAgent.EndEpisode();

        if (winPanel != null)
            winPanel.SetActive(true);

        if (winText != null)
        {
            winText.text = message;
            winText.gameObject.SetActive(true);
        }

        UpdateUI();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log(gameObject.name + " " + message);
    }

    public void RestartGameFromButton()
    {
        playerScore = 0;
        agentScore = 0;
        gameOver = false;
        isResetting = false;

        if (winPanel != null)
            winPanel.SetActive(false);

        if (winText != null)
            winText.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ResetEnvironment();
        UpdateUI();
    }

    public void ReturnToMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartMatch()
    {
        playerScore = 0;
        agentScore = 0;
        gameOver = false;

        if (winText != null)
            winText.gameObject.SetActive(false);

        ResetEnvironment();
        UpdateUI();
    }

    public void ResetEnvironment()
    {
        ResetEnvironmentWithPossession(PossessionTarget.Loose);
    }

    private void ResetEnvironmentWithPossession(PossessionTarget possessionTarget)
    {
        timer = 0f;
        looseBallTimer = 0f;

        ResetShotClock();

        if (possessionTarget == PossessionTarget.Loose)
        {
            ResetToNormalSpawns();

            if (ball != null && ballSpawn != null)
                ball.ResetBall(ballSpawn.position);
            else
                Debug.LogWarning(gameObject.name + " missing Ball or Ball Spawn reference.");
        }
        else if (possessionTarget == PossessionTarget.Agent)
        {
            ResetForAgentPossession();
        }
        else if (possessionTarget == PossessionTarget.Player)
        {
            ResetForPlayerPossession();
        }

        UpdateUI();
    }

    private void ResetToNormalSpawns()
    {
        if (mlAgent != null && agentSpawn != null)
            ResetAgent(mlAgent, agentSpawn);
        else
            Debug.LogWarning(gameObject.name + " missing ML Agent or Agent Spawn reference.");

        if (player != null && playerSpawn != null)
            ResetPlayer(player, playerSpawn);
    }

    private void ResetForAgentPossession()
    {
        if (mlAgent == null || player == null)
            return;

        Transform startTransform = GetRandomPossessionStart();

        if (startTransform == null)
            startTransform = agentSpawn;

        if (startTransform != null)
            ResetAgent(mlAgent, startTransform);

        FaceTransformTowardsHoop(mlAgent.transform);

        PlacePlayerInFrontOfAgent();

        if (ball != null)
        {
            ball.ResetBallToAgent(mlAgent);
            ResetShotClockForNewPossession();
        }
    }

    private void ResetForPlayerPossession()
    {
        if (mlAgent == null || player == null)
            return;

        Transform startTransform = GetRandomPossessionStart();

        if (startTransform == null)
            startTransform = playerSpawn;

        if (startTransform != null)
            ResetPlayer(player, startTransform);

        FaceTransformTowardsHoop(player.transform);

        PlaceAgentInFrontOfPlayer();

        if (ball != null)
        {
            ball.ResetBallToPlayer(player);
            ResetShotClockForNewPossession();
        }
    }

    private void FaceTransformTowardsHoop(Transform target)
    {
        if (target == null || shotTarget == null)
            return;

        Vector3 direction = shotTarget.position - target.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        target.rotation = Quaternion.LookRotation(direction);
    }

    private void PlaceAgentInFrontOfPlayer()
    {
        Vector3 defenderPosition =
            player.transform.position +
            player.transform.forward * defenderDistance;

        mlAgent.transform.position = defenderPosition;

        Vector3 lookDirection = player.transform.position - defenderPosition;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
            mlAgent.transform.rotation = Quaternion.LookRotation(lookDirection);

        Rigidbody agentRb = mlAgent.GetComponent<Rigidbody>();

        if (agentRb != null)
        {
            agentRb.linearVelocity = Vector3.zero;
            agentRb.angularVelocity = Vector3.zero;
        }
    }

    private void PlacePlayerInFrontOfAgent()
    {
        Vector3 defenderPosition =
            mlAgent.transform.position +
            mlAgent.transform.forward * defenderDistance;

        player.transform.position = defenderPosition;

        Vector3 lookDirection = mlAgent.transform.position - defenderPosition;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
            player.transform.rotation = Quaternion.LookRotation(lookDirection);

        Rigidbody playerRb = player.GetComponent<Rigidbody>();

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }
    }

    private void ResetAgent(BasketballAgent agent, Transform spawn)
    {
        Rigidbody agentRb = agent.GetComponent<Rigidbody>();

        agent.transform.position = spawn.position;
        agent.transform.rotation = spawn.rotation;

        if (agentRb != null)
        {
            agentRb.linearVelocity = Vector3.zero;
            agentRb.angularVelocity = Vector3.zero;
        }
    }

    private void ResetPlayer(PlayerBasketballController playerController, Transform spawn)
    {
        Rigidbody playerRb = playerController.GetComponent<Rigidbody>();

        playerController.transform.position = spawn.position;
        playerController.transform.rotation = spawn.rotation;

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }
    }

    private void UpdateUI()
    {
        if (playerScoreText != null)
            playerScoreText.text = "Player: " + playerScore;

        if (agentScoreText != null)
            agentScoreText.text = "Agent: " + agentScore;

        if (possessionText != null && ball != null)
        {
            if (ball.playerOwner != null)
                possessionText.text = "Player has the ball";
            else if (ball.agentOwner != null)
                possessionText.text = "Agent has the ball";
            else
                possessionText.text = "Loose ball";
        }

        if (shotClockText != null)
        {
            if (shotClockRunning)
                shotClockText.text = "Shot Clock: " + Mathf.CeilToInt(shotClockTimer);
            else
                shotClockText.text = "Shot Clock: --";
        }
    }

    private Transform GetRandomPossessionStart()
    {
        if (possessionStarts == null || possessionStarts.Length == 0)
            return null;

        int randomIndex = Random.Range(0, possessionStarts.Length);
        return possessionStarts[randomIndex];
    }
}