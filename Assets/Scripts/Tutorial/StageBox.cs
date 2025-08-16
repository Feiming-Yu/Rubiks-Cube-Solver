using System.Collections;
using UI;
using UnityEngine;

namespace Tutorial
{
    public class StageBox : MonoBehaviour
    {
        // singleton instance
        public static StageBox Instance;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(Instance);
        }
        
        [SerializeField] private TMPro.TextMeshProUGUI stage, heading, subHeading, body;

        [SerializeField] private GameObject showButton;

        private readonly Vector3 HIDDEN_POS = new(-160f, 8f, 0f);
        private readonly Vector3 VISIBLE_POS = new(-62.5f, 8f, 0f);

        private const float ANIMATION_TIME = 0.1f;

        public void Show()
        {
            UpdateInformation();
            StartCoroutine(Animate(transform.localPosition, VISIBLE_POS, ANIMATION_TIME));
        }

        public void Hide(bool useTutorials = true)
        {
            StartCoroutine(Animate(transform.localPosition, HIDDEN_POS, ANIMATION_TIME, true, useTutorials));
        }

        public void UpdateInformation()
        {
            if (!Manager.Instance.useTutorials)
                return;

            Stage currentStage = Cube.Instance.GetCurrentStage();

            stage.text = $"Stage {currentStage.Index + 1}";
            heading.text = currentStage.Name;
            subHeading.text = currentStage.FriendlyName;
            body.text = currentStage.Description;
        }

        private IEnumerator Animate(Vector3 from, Vector3 to, float time, bool hide = false, bool useTutorials = true)
        {
            float elapsed = 0f;

            while (elapsed < time)
            {
                transform.localPosition = Vector3.Lerp(from, to, elapsed / time);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = to;

            showButton.SetActive(hide && useTutorials);
        }
    }
}
