using System.Collections;
using UnityEngine;

public class BackboardFeedback : MonoBehaviour
{
    public Renderer backboardRenderer;

    [Header("Colours")]
    public Color normalColour = Color.white;
    public Color scoreColour = Color.green;
    public Color missColour = Color.red;

    [Header("Timing")]
    public float feedbackTime = 0.6f;

    private Material materialInstance;
    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (backboardRenderer == null)
            backboardRenderer = GetComponent<Renderer>();

        if (backboardRenderer != null)
            materialInstance = backboardRenderer.material;

        SetColour(normalColour);
    }

    public void ShowScore()
    {
        ShowColour(scoreColour);
    }

    public void ShowMiss()
    {
        ShowColour(missColour);
    }

    private void ShowColour(Color colour)
    {
        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(FeedbackRoutine(colour));
    }

    private IEnumerator FeedbackRoutine(Color colour)
    {
        SetColour(colour);

        yield return new WaitForSeconds(feedbackTime);

        SetColour(normalColour);
    }

    private void SetColour(Color colour)
    {
        if (materialInstance == null)
            return;

        if (materialInstance.HasProperty("_BaseColor"))
        {
            materialInstance.SetColor("_BaseColor", colour);
        }
        else
        {
            materialInstance.color = colour;
        }
    }
}