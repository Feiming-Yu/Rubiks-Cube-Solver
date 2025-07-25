using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI
{
    public class HorizontalMoveDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject moveTextPrefab;

        private int _currentIndex;
        private int _moveCount;
        private List<string> _moveList;

        /// <summary>
        /// Displays a list of cube moves on the screen by instantiating and styling text objects.
        /// Clears any existing moves before displaying the new list.
        /// </summary>
        public void DisplayMoves(List<string> moves)
        {
            // clear any previously displayed move texts from the ui
            ClearDisplay();

            _moveCount = moves.Count;

            for (int i = 0; i < moves.Count; i++)
            {
                GameObject moveObj = Instantiate(moveTextPrefab, transform);

                // get the text component and set it to the move string
                TextMeshProUGUI moveText = moveObj.GetComponent<TextMeshProUGUI>();
                moveText.text = moves[i];

                // apply visual style, highlighting the current move if applicable
                SetTextStyle(moveText, i == _currentIndex, i, true);
            }
        }

        /// <summary>
        /// Clears all moves, resets counters, and repositions the display.
        /// </summary>
        public void ClearDisplay()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            // reset counters
            _moveCount = _currentIndex = 0;

            // reset horizontal position
            var pos = transform.localPosition;
            transform.localPosition = new Vector3(41, pos.y, pos.z);
        }


        private void SetTextStyle(TMP_Text text, bool isCurrentMove, int index, bool postAnimation, bool isProgress = true)
        {
            if (isCurrentMove)
                SetFocusedStyle(text);
            else
            {
                text.fontSize = 8;
                int distanceFromCentre = isProgress 
                    ? Mathf.Clamp(index - _currentIndex, -6, 5) 
                    : Mathf.Clamp(index - _currentIndex, -5, 6);

                float preAnimDist = distanceFromCentre > 0 == isProgress ? 5f : 6f;
                float position = Mathf.Abs((float)distanceFromCentre) / (postAnimation ? 5f : preAnimDist);
            
                float alpha = Mathf.Lerp(0.2f, 0f, position);
                text.fontStyle = FontStyles.Normal;
                text.color = new Color(0.7f, 0.7f, 0.7f, alpha);
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

        private void SetFocusedStyle(TMP_Text text)
        {
            text.fontSize = 12;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
        }

        public void Progress()
        {
            ChangeMove(true);
        }
    
        public void Regress()
        {
            ChangeMove(false);
        }

        private void ChangeMove(bool progress)
        {

            if (_moveCount == 0)
                return;

            int direction = progress ? 1 : -1;

            var pos = transform.localPosition;

            float duration = 0.25f / Cube.Instance.animationSpeed;
            _currentIndex += direction;

            StyleSideText(false, progress);
            StyleFocusedText();
            StartCoroutine(ShiftListTransform(new Vector3(pos.x + (direction * -18), pos.y, pos.z), duration, progress));
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

            transform.localPosition = end; // ensure final position is set exactly

            StyleSideText(true, isProgress);
        }

        public void ToStart()
        {
            SetIndexAndPosition(0);
        }

        public void ToEnd()
        {
            SetIndexAndPosition(_moveCount);
        }

        private void SetIndexAndPosition(int index)
        {
            _currentIndex = index;
            StyleFocusedText();
            StyleSideText(true);

            var pos = transform.localPosition;
            float x = (index == 0) ? 41 : 41 + (index * -18);
            transform.localPosition = new Vector3(x, pos.y, pos.z);
        }
    }
}
