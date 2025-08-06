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

        private readonly Vector3 _hiddenPos = new(-160f, 8f, 0f);
        private readonly Vector3 _visiblePos = new(-62.5f, 8f, 0f);

        public void Show()
        {
            UpdateInformation();
            StartCoroutine(Animate(transform.localPosition, _visiblePos, 0.1f));
        }

        public void Hide(bool useTutorials = true)
        {
            StartCoroutine(Animate(transform.localPosition, _hiddenPos, 0.1f, true, useTutorials));
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

            if (hide && useTutorials)
                showButton.SetActive(true);
            else
                showButton.SetActive(false);
        }
    }
}
