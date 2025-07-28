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
            // Clear any previously displayed move texts from the ui
            ClearDisplay();

            _moveCount = moves.Count;

            for (int i = 0; i < moves.Count; i++)
            {
                GameObject moveObj = Instantiate(moveTextPrefab, transform);

                // Get the text component and set it to the move string
                TextMeshProUGUI moveText = moveObj.GetComponent<TextMeshProUGUI>();
                moveText.text = moves[i];

                // Apply visual style, highlighting the current move if applicable
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

            // Reset counters
            _moveCount = _currentIndex = 0;

            // Reset horizontal position
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
                
                // During animation, show the last move before destroying at the end of animation
                int distanceFromCentre = isProgress 
                    ? Mathf.Clamp(index - _currentIndex, -6, 5) 
                    : Mathf.Clamp(index - _currentIndex, -5, 6);
                float preAnimDist = distanceFromCentre > 0 == isProgress ? 5f : 6f;
                float position = Mathf.Abs((float)distanceFromCentre) / (postAnimation ? 5f : preAnimDist);
            
                float alpha = Manager.Instance.currentThemeIndex == 0
                    ? Mathf.Lerp(0.2f, 0f, position)
                    : Mathf.Lerp(0.5f, 0f, position);
                text.color = Manager.Instance.currentThemeIndex == 0 
                    ? new Color(0.7f, 0.7f, 0.7f, alpha)
                    : new Color(0.2f, 0.2f, 0.2f, alpha);
                
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
            // The cube is solved, no moves left
            if (_currentIndex == _moveCount)
                return;

            var text = transform.GetChild(_currentIndex).GetComponent<TextMeshProUGUI>();

            SetFocusedStyle(text);
        }

        private void SetFocusedStyle(TMP_Text text)
        {
            text.fontSize = 12;
            text.fontStyle = FontStyles.Bold;
            text.color = Manager.Instance.currentThemeIndex == 0 ? Color.white : Color.black;
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
            if (_moveCount == 0) return;

            
            int direction = progress ? 1 : -1;
            _currentIndex += direction;

            // Styling before animation
            StyleSideText(false, progress);
            StyleFocusedText();
            
            var pos = transform.localPosition;
            float duration = 0.15f / Cube.Instance.animationSpeed;
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

            transform.localPosition = end; // Ensure final position is set exactly

            // Styling after animation
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
            
            // No animation needed, just use post animation styling
            StyleFocusedText();
            StyleSideText(true);

            var pos = transform.localPosition;
            
            // 41 is initial x-coordinate
            float x = 41f + (index * -18);
            transform.localPosition = new Vector3(x, pos.y, pos.z);
        }

        public void UpdateColours()
        {
            for (int i = 0; i < _moveCount; i++)
            {
                // Get the text component and set it to the move string
                TextMeshProUGUI moveText = transform.GetChild(i).GetComponent<TextMeshProUGUI>();

                // Apply visual style, highlighting the current move if applicable
                SetTextStyle(moveText, i == _currentIndex, i, true);
            }
        }
    }
}
