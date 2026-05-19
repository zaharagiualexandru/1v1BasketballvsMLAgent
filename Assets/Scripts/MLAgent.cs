using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MLAgent : Agent
{
    public Transform targetObject;
    public MeshRenderer GroundRenderer;
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public override void OnEpisodeBegin()
    {
        //Assign random position to the agent and the target at the beginning of each episode
        transform.localPosition = new Vector3(Random.Range(-4.4f, -1f), 1.1f, Random.Range(-4.4f, 4.4f));
        targetObject.localPosition = new Vector3(Random.Range(1f, 4.3f), 1.1f, Random.Range(-4.3f, 4.3f));

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        //Assign actions to the agent movement
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        transform.position += new Vector3(moveX, 0, moveZ) * Time.deltaTime * 5f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Add oversations to the sensor
        sensor.AddObservation(new Vector2(transform.localPosition.x, transform.localPosition.z));
        sensor.AddObservation(new Vector2(targetObject.localPosition.x, targetObject.localPosition.z));
    }

    private void OnTriggerEnter(Collider other)
    {
        //Wall -> penalty
        if(other.tag == "Wall")
        {
            Debug.Log("Hit the wall");
            AddReward(-1f);
            GroundRenderer.material.color = Color.red;
            EndEpisode();
        }
        //Target -> reward
        else if(other.tag == "Target")
        {
            Debug.Log("Hit the target");
            AddReward(10f);
            GroundRenderer.material.color = Color.green;
            EndEpisode();
        }
    }
}