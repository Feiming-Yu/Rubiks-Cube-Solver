using System.Collections;
using UnityEngine;

namespace Tutorial
{
    public class CubeErrorBox : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI body;

        private static readonly Vector3 HiddenPos = new(-160f, -15f, 0f);
        private static readonly Vector3 VisiblePos = new(-64.75f, -15f, 0f);

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
            StartCoroutine(Animate(transform.localPosition, VisiblePos, 0.1f));
        }

        public void Hide()
        {
            StartCoroutine(Animate(transform.localPosition, HiddenPos, 0.1f));
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
}
