using System.Collections;
using UnityEngine;

public class NetReaction : MonoBehaviour
{
    [Header("Net Movement")]
    public float wobbleAmount = 0.12f;
    public float wobbleSpeed = 18f;
    public float wobbleDuration = 0.6f;

    [Header("Scale Squash")]
    public float squashAmount = 0.15f;

    private Vector3 startLocalPosition;
    private Vector3 startLocalScale;
    private Coroutine wobbleRoutine;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        startLocalScale = transform.localScale;
    }

    public void PlayNetReaction()
    {
        if (wobbleRoutine != null)
            StopCoroutine(wobbleRoutine);

        wobbleRoutine = StartCoroutine(WobbleNet());
    }

    private IEnumerator WobbleNet()
    {
        float timer = 0f;

        while (timer < wobbleDuration)
        {
            timer += Time.deltaTime;

            float strength = 1f - (timer / wobbleDuration);
            float wobble = Mathf.Sin(timer * wobbleSpeed) * wobbleAmount * strength;

            transform.localPosition = startLocalPosition + new Vector3(wobble, -Mathf.Abs(wobble) * 0.5f, 0f);

            float squash = 1f - Mathf.Abs(wobble) * squashAmount;
            transform.localScale = new Vector3(
                startLocalScale.x + Mathf.Abs(wobble) * squashAmount,
                startLocalScale.y * squash,
                startLocalScale.z + Mathf.Abs(wobble) * squashAmount
            );

            yield return null;
        }

        transform.localPosition = startLocalPosition;
        transform.localScale = startLocalScale;
    }
}