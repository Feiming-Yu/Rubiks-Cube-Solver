using System.Collections;
using UnityEngine;

namespace Tutorial
{
    public class CubeErrorBox : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI body;

        private static readonly Vector3 HIDDEN_POS = new(-160f, -15f, 0f);
        private static readonly Vector3 VISIBLE_POS = new(-64.75f, -15f, 0f);

        private const float ANIMATION_TIME = 0.1f;

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
            StartCoroutine(Animate(transform.localPosition, VISIBLE_POS, ANIMATION_TIME));
        }

        public void Hide()
        {
            StartCoroutine(Animate(transform.localPosition, HIDDEN_POS, ANIMATION_TIME));
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
