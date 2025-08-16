using System.Collections;
using System.Collections.Generic;
using TMPro;
using Tutorial;
using UnityEngine;

namespace UI
{
    public class HorizontalMoveDisplay : MonoBehaviour
    {
        #region Constants

        private const float INITIAL_X = 41f;
        private const float MOVE_OFFSET_X = -18f;
        private const int MAX_FONT_SIZE = 12;
        private const int MIN_FONT_SIZE = 8;
        private const float ANIMATION_BASE_TIME = 0.15f;

        // Distance clamps
        private const int DISTANCE_CLAMP_NEGATIVE_PROGRESS = -6;
        private const int DISTANCE_CLAMP_POSITIVE_PROGRESS = 5;
        private const int DISTANCE_CLAMP_NEGATIVE_REGRESS = -5;
        private const int DISTANCE_CLAMP_POSITIVE_REGRESS = 6;

        // Pre-animation distances
        private const float PRE_ANIM_DIST_POSITIVE = 5f;
        private const float PRE_ANIM_DIST_NEGATIVE = 6f;

        // Alpha values
        private const float ALPHA_THEME0_MIN = 0.2f;
        private const float ALPHA_THEME0_MAX = 0f;
        private const float ALPHA_THEME1_MIN = 0.5f;
        private const float ALPHA_THEME1_MAX = 0f;
        private const float THEME0_RGB = 0.7f;
        private const float THEME1_RGB = 0.2f;

        #endregion

        [SerializeField] private GameObject moveTextPrefab;

        private int _currentIndex;
        private int _moveCount;
        private List<string> _moves;

        /// <summary>
        /// Displays a list of cube moves on the screen by instantiating and styling text objects.
        /// Clears any existing moves before displaying the new list.
        /// </summary>
        public void DisplayMoves(List<string> moves)
        {
            ClearDisplay();

            _moves = new List<string>(moves);
            _moveCount = moves.Count;
            _currentIndex = Cube.Instance.GetCurrentIndex();

            if (_currentIndex == _moveCount
                && Manager.Instance.useStages
                && !Cube.Instance.LastSequence)
            {
                _currentIndex--;
            }

            for (int i = 0; i < moves.Count; i++)
            {
                GameObject moveObj = Instantiate(moveTextPrefab, transform);
                TextMeshProUGUI moveText = moveObj.GetComponent<TextMeshProUGUI>();
                moveText.text = moves[i];

                SetTextStyle(moveText, i == _currentIndex, i, true);
            }

            SetPosition(_currentIndex);

            StageBox.Instance.UpdateInformation();
            SequenceBox.Instance.UpdateInformation();
        }

        public void ClearDisplay()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            _moveCount = 0;
            _currentIndex = 0;

            var pos = transform.localPosition;
            transform.localPosition = new Vector3(INITIAL_X, pos.y, pos.z);
        }

        public string GetCurrentDisplayedMove()
        {
            return _currentIndex == _moveCount ? "" : _moves[_currentIndex];
        }

        private void SetTextStyle(TMP_Text text, bool isCurrentMove, int index, bool postAnimation, bool isProgress = true)
        {
            if (isCurrentMove)
                SetFocusedStyle(text);
            else
            {
                text.fontSize = MIN_FONT_SIZE;

                int distanceFromCentre = isProgress
    ? Mathf.Clamp(index - _currentIndex, DISTANCE_CLAMP_NEGATIVE_PROGRESS, DISTANCE_CLAMP_POSITIVE_PROGRESS)
    : Mathf.Clamp(index - _currentIndex, DISTANCE_CLAMP_NEGATIVE_REGRESS, DISTANCE_CLAMP_POSITIVE_REGRESS);

                float preAnimDist = distanceFromCentre > 0 == isProgress ? PRE_ANIM_DIST_POSITIVE : PRE_ANIM_DIST_NEGATIVE;
                float position = Mathf.Abs(distanceFromCentre) / (postAnimation ? PRE_ANIM_DIST_POSITIVE : preAnimDist);

                float alpha = Manager.Instance.currentThemeIndex == 0
                    ? Mathf.Lerp(ALPHA_THEME0_MIN, ALPHA_THEME0_MAX, position)
                    : Mathf.Lerp(ALPHA_THEME1_MIN, ALPHA_THEME1_MAX, position);

                text.color = Manager.Instance.currentThemeIndex == 0
                    ? new Color(THEME0_RGB, THEME0_RGB, THEME0_RGB, alpha)
                    : new Color(THEME1_RGB, THEME1_RGB, THEME1_RGB, alpha);


                text.fontStyle = FontStyles.Normal;
            }
        }

        private void StyleSideText(bool postAnimation, bool isProgress = true)
        {
            for (int i = 0; i < _moveCount; i++)
            {
                if (i == _currentIndex)
                    continue;

                TextMeshProUGUI moveText = transform.GetChild(i).GetComponent<TextMeshProUGUI>();
                SetTextStyle(moveText, false, i, postAnimation, isProgress);
            }
        }

        private void StyleFocusedText()
        {
            if (_currentIndex == _moveCount)
                return;

            var text = transform.GetChild(_currentIndex).GetComponent<TextMeshProUGUI>();
            SetFocusedStyle(text);
        }

        private static void SetFocusedStyle(TMP_Text text)
        {
            text.fontSize = MAX_FONT_SIZE;
            text.fontStyle = FontStyles.Bold;
            text.color = Manager.Instance.currentThemeIndex == 0 ? Color.white : Color.black;
        }

        public void Progress() => ChangeMove(true);
        public void Regress() => ChangeMove(false);

        private void ChangeMove(bool progress)
        {
            if (_moveCount == 0) return;

            int direction = progress ? 1 : -1;
            _currentIndex += direction;

            StyleSideText(false, progress);
            StyleFocusedText();

            var pos = transform.localPosition;
            float duration = ANIMATION_BASE_TIME / Cube.Instance.animationSpeed;
            StartCoroutine(ShiftListTransform(new Vector3(pos.x + (direction * MOVE_OFFSET_X), pos.y, pos.z), duration, progress));
        }

        private IEnumerator ShiftListTransform(Vector3 end, float time, bool isProgress = true)
        {
            Vector3 start = transform.localPosition;
            float elapsed = 0;

            while (elapsed < time)
            {
                transform.localPosition = Vector3.Lerp(start, end, elapsed / time);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = end;
            StyleSideText(true, isProgress);
        }

        public void ToStart() => SetIndexAndPosition(0);
        public void ToEnd() => SetIndexAndPosition(_moveCount);

        private void SetIndexAndPosition(int index)
        {
            _currentIndex = index;
            StyleFocusedText();
            StyleSideText(true);
            SetPosition(index);
        }

        private void SetPosition(int index)
        {
            var pos = transform.localPosition;
            float x = INITIAL_X + (index * MOVE_OFFSET_X);
            transform.localPosition = new Vector3(x, pos.y, pos.z);
        }

        public void UpdateColours()
        {
            for (int i = 0; i < _moveCount; i++)
            {
                TextMeshProUGUI moveText = transform.GetChild(i).GetComponent<TextMeshProUGUI>();
                SetTextStyle(moveText, i == _currentIndex, i, true);
            }
        }
    }
}
