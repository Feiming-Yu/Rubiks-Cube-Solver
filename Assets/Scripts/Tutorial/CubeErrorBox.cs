using System.Collections;
using UnityEngine;

public class CubeErrorBox : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI body;

    private readonly Vector3 hiddenPos = new(-160f, -15f, 0f);
    private readonly Vector3 visiblePos = new(-64.75f, -15f, 0f);

    // singleton instance
    public static CubeErrorBox Instance;

    private void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(Instance);
    }

    private void UpdateMessage()
    {
        body.text = Engine.Validation.InvalidCubeException.Message;
    }

    public void Show()
    {
        UpdateMessage();
        StartCoroutine(Animate(transform.localPosition, visiblePos, 0.1f));
    }

    public void Hide()
    {
        StartCoroutine(Animate(transform.localPosition, hiddenPos, 0.1f));
    }

    private IEnumerator Animate(Vector3 from, Vector3 to, float time)
    {
        float elapsed = 0f;

        while (elapsed < time)
        {
            transform.localPosition = Vector3.Lerp(from, to, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = to;
    }

}
