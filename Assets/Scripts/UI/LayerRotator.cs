using UnityEngine;
using static System.Math;

namespace UI
{

    public class LayerRotator : MonoBehaviour
    {
        private Vector3 _dragAxis;
        private bool _isCalculated, _isOrientating;

        private Vector2 _initialClickPos = Vector2.zero;
        private Vector2 _lastMousePos = Vector2.zero;

        private static Vector2 MousePosition =>
            // Subtracts by half of the screen size
            // (0, 0) is the centre of the screen
            // Useful for checking which side the mouse is on (i.e. negative x means left side)
            Input.mousePosition - new Vector3(Screen.width / 2f, Screen.height / 2f);

        private void OnMouseEnter()
        {
            HandleLayerRotation();
            
        }

        private void HandleLayerRotation()
        {
            HandleMouseDown();
            HandleMouseMove();
        }

        private void HandleMouseDown()
        {
            if (!Input.GetMouseButtonDown(1) || _isCalculated || _isOrientating || Manager.Instance.isWindowOpen)
                return;

            _lastMousePos = _initialClickPos = MousePosition;

            _isOrientating = true;
        }

        private void HandleMouseMove()
        {
            if (!_isOrientating) return;

            Vector2 newMousePos = MousePosition;

            // Calculation only begins if the mouse has moved
            if (newMousePos == _initialClickPos) return;
            // Locks the direction of the drag until button is released
            if (!_isCalculated)
                CalculateDragDirection(newMousePos, _initialClickPos);

            // Checks if the change in mouse position is significant
            if (!_isCalculated) return;

            // x-coordinate is more sensitive
            float rotation = _dragAxis.y == 0 ? newMousePos.y - _lastMousePos.y : (newMousePos.x - _lastMousePos.x) / 1.5f;
            rotation *= Time.deltaTime * 40;

            print(rotation);
        }

        private void CalculateDragDirection(Vector2 newMousePos, Vector2 initialClickPos)
        {
            var delta = CalculateDeltaVector(newMousePos, initialClickPos);

            // Checks if the vector change is significant
            // True if beyond the threshold of 10 pixels
            // Reduces noise and accidental input
            if (delta.magnitude < 10f) return;

            var modDelta = CalculateModulusVector(delta);

            // If the change in x-position is more significant
            if (modDelta.x > modDelta.y)
                // Multiply by -1 as direction of rotation is opposite to mouse movement
                _dragAxis = new Vector3(0, 1, 0) * -1;
            else
            {
                bool isLeftSide = initialClickPos.x < 0;
                _dragAxis = isLeftSide ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1);
            }

            // Locks the drag direction to avoid a non-one-dimensional rotation value
            _isCalculated = true;
        }

        /// <summary>
        /// Applies the modulus function on the x and y component.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private static Vector2 CalculateModulusVector(Vector2 v) => new Vector2(Abs(v.x), Abs(v.y));

        private static Vector2 CalculateDeltaVector(Vector2 newMousePos, Vector2 initialClickPos)
            => newMousePos - initialClickPos;

    }
}
