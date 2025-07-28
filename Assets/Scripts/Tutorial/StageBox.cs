using System;
using System.Collections;
using UnityEngine;

public class StageBox : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI stage, heading, subHeading, body;

    [SerializeField] GameObject showButton;

    private readonly Vector3 hiddenPos = new(-160f, 8f, 0f);
    private readonly Vector3 visiblePos = new(-62.5f, 8f, 0f);

    public void Show()
    {
        showButton.SetActive(false);

        StartCoroutine(Animate(transform.localPosition, visiblePos, 0.1f));
    }

    public void Hide()
    {
        StartCoroutine(Animate(transform.localPosition, hiddenPos, 0.1f, true));
    }

    private IEnumerator Animate(Vector3 from, Vector3 to, float time, bool hide = false)
    {
        float elapsed = 0f;

        while (elapsed < time)
        {
            transform.localPosition = Vector3.Lerp(from, to, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = to;

        if (hide)
            showButton.SetActive(true);
    }
}
