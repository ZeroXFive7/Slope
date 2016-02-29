using UnityEngine;
using System.Collections;

public class BoardTrailRenderers : MonoBehaviour
{
    [System.Serializable]
    public struct TrailColors
    {
        public string Name;
        public Color FrontColor;
        public Color BackColor;
    };

    [SerializeField]
    private TrailRenderer frontTrailRenderer = null;
    [SerializeField]
    private TrailRenderer backTrailRenderer = null;

    private bool isResetting = false;

    public TrailColors Colors
    {
        set
        {
            frontTrailRenderer.material.color = value.FrontColor;
            frontTrailRenderer.material.SetColor("_EmissionColor", value.FrontColor);

            backTrailRenderer.material.color = value.BackColor;
            backTrailRenderer.material.SetColor("_EmissionColor", value.BackColor);
        }
    }

    public void Reset()
    {
        if (!isResetting)
        {
            StartCoroutine(ResetCoroutine());
        }
    }

    private IEnumerator ResetCoroutine()
    {
        isResetting = true;

        float trailTime = frontTrailRenderer.time;

        frontTrailRenderer.time = -1.0f;
        backTrailRenderer.time = -1.0f;

        yield return new WaitForEndOfFrame();

        frontTrailRenderer.time = trailTime;
        backTrailRenderer.time = trailTime;

        isResetting = false;
    }
}
