using UnityEngine;
using UnityEngine.UI;
using static System.MathF;

namespace UI
{
    public class Orientator : MonoBehaviour
    {
        [SerializeField] private Transform cube;
        [SerializeField] private Camera cam;
        [SerializeField] private int snappingSpeed;
        [SerializeField] private float rotationSpeed = 5f;

        private bool _isOrientating;
        private bool _isCalculated;
        private bool _isSnapping;

        private Vector2 _initialClickPos = Vector2.zero;
        private Vector2 _lastMousePos = Vector2.zero;

        private Vector3 _dragAxis;

        private Quaternion _quantisedRotation;

        private static Vector2 MousePosition =>
            // Subtracts by half of the screen size
            // (0, 0) is the centre of the screen
            // Useful for checking which side the mouse is on (i.e. negative x means left side)
            Input.mousePosition - new Vector3(Screen.width / 2f, Screen.height / 2f);

        public void UpdateRotationSpeed(Slider speedSlider) => rotationSpeed = speedSlider.value;

        private void Update()
        {
            HandleRMBRelease();

            HandleRotationSnapping();

            HandleRMBPress();

            if (_isOrientating && !Manager.Instance.isWindowOpen)
                HandleMouseMove();
        }

        private void HandleRMBRelease()
        {
            if (Input.GetMouseButton(1)) return;

            // Reset flags
            _isOrientating = _isCalculated = Cube.Instance.isOrientating = false;

            _quantisedRotation = Quaternion.Euler(QuantiseVector(cube.eulerAngles));

            // Start the animation to snap the cube to a quantised rotation
            _isSnapping = true;
        }

        private void HandleRMBPress()
        {
            if (!Input.GetMouseButtonDown(1) || _isCalculated || _isOrientating || Manager.Instance.isWindowOpen) 
                return;

            _lastMousePos = _initialClickPos = MousePosition;

            _isOrientating = Cube.Instance.isOrientating = true;
        }

        /// <summary>
        /// Rotates the transform to a discrete value
        /// </summary>
        private void HandleRotationSnapping()
        {
            if (!_isSnapping) return;

            // Animates the cube from its current rotation to the quantised rotation
            cube.rotation = Quaternion.RotateTowards(cube.rotation, _quantisedRotation, snappingSpeed * Time.deltaTime);

            // Stop animation once the current cube has reached quantised rotation
            if (cube.rotation.eulerAngles == _quantisedRotation.eulerAngles)
                _isSnapping = false;
        }

        private void HandleMouseMove()
        {
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
            rotation *= Time.deltaTime * rotationSpeed * 40;

            cube.Rotate(_dragAxis, rotation, Space.World);

            _lastMousePos = newMousePos;
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

        /// <summary>
        /// Rounds each component to the nearest 90
        /// The cube therefore remains in the same shape on the screen
        /// </summary>
        private static Vector3 QuantiseVector(Vector3 vector)
        {
            return new Vector3(
                Mathf.Round(vector.x / 90f),
                Mathf.Round(vector.y / 90f),
                Mathf.Round(vector.z / 90f)
            ) * 90f;
        }
    }
}
