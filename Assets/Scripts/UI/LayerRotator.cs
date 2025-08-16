using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static System.MathF;
using static UI.Cube;
using static UI.Square;

namespace UI
{
    public class LayerRotator : MonoBehaviour
    {
        #region Constants

        private readonly Vector3 STARTING_ROTATON = new(0, 90, 0);
        private readonly Vector2 X_AXIS = new(1, 0);
        private readonly Vector2 Y_AXIS = new(0, 1);
        private const int ROTATION_MULTIPLIER = 40;
        private const int L_R_INDEX = 0;
        private const int U_D_INDEX = 1;
        private const int B_F_INDEX = 2;
        private const float SMALL_ANGLE_TOLERANCE = 0.1f;
        private const int DRAG_THRESHOLD = 10;

        #endregion

        #region Graphics

        private bool _isRotating;
        private bool _isHovering;
        private bool _isCalculated;
        private bool _isSnapping;

        private Vector2 _initialClickPos = Vector2.zero;
        private Vector2 _lastMousePos = Vector2.zero;

        private Vector3 _dragAxis;

        private Vector3 _quantisedRotation;

        private Transform _rotationPlane;

        private static int _rotatingFaceIndex = -1;

        public static bool IsBusy { get; private set; }

        private Vector3 _planeEulerAngles;

        private Vector3 PlaneEulerAngles
        {
            get => _planeEulerAngles;
            set
            {
                _planeEulerAngles = NormaliseVector(value);
                _rotationPlane.localEulerAngles = _planeEulerAngles;
            }
        }

        private void Start()
        {
            _rotationPlane = Instance.transform.Find("Rotation Plane");
        }

        private void OnMouseEnter()
        {
            _isHovering = true;
            Manager.Instance.isOnCube = true;
        }

        private void OnMouseExit()
        {
            _isHovering = false;
            Manager.Instance.isOnCube = false;
        }

        private void Update()
        {
            HandleRMBRelease();

            HandleRotationSnapping();

            HandleRMBPress();

            if (_isRotating)
                HandleMouseMove();
        }

        /// <summary>
        /// Handle right mouse button release - trigger snapping if rotation occurred.
        /// </summary>
        private void HandleRMBRelease()
        {
            if (Input.GetMouseButton(1) || !_isRotating || !_isCalculated) return;

            IsBusy = _isRotating = _isCalculated = false;

            _quantisedRotation = NormaliseVector(QuantiseVector(PlaneEulerAngles));

            // Start the animation to snap the cube to a quantised rotation
            IsBusy = _isSnapping = true;
        }

        /// <summary>
        /// Handle right mouse button press - start rotation if hovering over sticker.
        /// </summary>
        private void HandleRMBPress()
        {
            if (!Input.GetMouseButtonDown(1) || 
                !_isHovering || 
                _isCalculated || 
                Manager.IsAnimatingInputs || 
                Cube.Instance.IsSolving() ||
                GetComponent<Square>().IsCentre) return;

            _lastMousePos = _initialClickPos = GetMousePosition();

            PlaneEulerAngles = _rotationPlane.localEulerAngles;

            // Lock rotation state
            IsBusy = _isRotating = true;
        }

        /// <summary>
        /// Rotates the transform to a discrete value
        /// </summary>
        private void HandleRotationSnapping()
        {
            if (!_isSnapping) return;

            // Determine target rotation
            Vector3 targetEuler = NormaliseVector(_quantisedRotation);

            // Smoothly move current rotation toward target
            Vector3 newEuler = new Vector3(
                Mathf.MoveTowardsAngle(PlaneEulerAngles.x, targetEuler.x, Time.deltaTime * CubeRotator.Instance.snappingSpeed),
                Mathf.MoveTowardsAngle(PlaneEulerAngles.y, targetEuler.y, Time.deltaTime * CubeRotator.Instance.snappingSpeed),
                Mathf.MoveTowardsAngle(PlaneEulerAngles.z, targetEuler.z, Time.deltaTime * CubeRotator.Instance.snappingSpeed)
            );

            PlaneEulerAngles = newEuler;

            // Stop animation once the current cube has reached quantised rotation
            if (NormaliseVector(PlaneEulerAngles) != NormaliseVector(_quantisedRotation)) return;
            
            IsBusy = _isSnapping = false;

            // Reset rotation
            PlaneEulerAngles = STARTING_ROTATON;

            UngroupPieces();

            // Finalize rotation on cube model
            Instance.MoveOnCubie(GetMoveFromRotation());
            Instance.UpdateFacelet();
            Instance.SetColours(Instance.GetFacelet());
        }

        /// <summary>
        /// Handle mouse movement during an active rotation.
        /// </summary>
        private void HandleMouseMove()
        {
            Vector2 newMousePos = GetMousePosition();

            // Calculation only begins if the mouse has moved
            if (newMousePos == _initialClickPos) return;
            // Locks the direction of the drag until button is released
            if (!_isCalculated)
                CalculateDragDirection(newMousePos, _initialClickPos);

            // Checks if the change in mouse position is significant
            if (!_isCalculated) return;

            float rotation = GetRotationMagnitude() * Time.deltaTime * CubeRotator.Instance.rotationSpeed * ROTATION_MULTIPLIER;

            PlaneEulerAngles += GetRotationAxis(ColourToFace(_rotatingFaceIndex)) * rotation;

            _lastMousePos = newMousePos;
        }

        /// <summary>
        /// Determine which axis to rotate around based on initial drag.
        /// Locks the axis until mouse is released.
        /// </summary>
        private void CalculateDragDirection(Vector2 newMousePos, Vector2 initialClickPos)
        {
            Vector2 delta = CalculateDeltaVector(newMousePos, initialClickPos);

            // Checks if the vector change is significant
            // True if beyond the threshold of 10 pixels
            // Reduces noise and accidental input
            if (delta.magnitude < DRAG_THRESHOLD) return;

            Vector3 worldDelta = new(delta.x, delta.y, 0f);
            Vector3 localDelta = Instance.transform.InverseTransformDirection(Camera.main.transform.TransformDirection(worldDelta));
            var modDelta = CalculateModulusVector(localDelta);

            // If the change in x-position is more significant
            if (modDelta.x > modDelta.y)
                // Multiply by -1 as direction of rotation is opposite to mouse movement
                _dragAxis = Y_AXIS * -1;
            else
                _dragAxis = X_AXIS;

            GroupPieces(localDelta);

            // Locks the drag direction to avoid a two-dimensional rotation value
            _isCalculated = true;
        }

        /// <summary>
        /// Applies the modulus function on the x and y component.
        /// </summary>
        private static Vector2 CalculateModulusVector(Vector2 v)
        {
            return new Vector2(Abs(v.x), Abs(v.y));
        }

        private static Vector2 CalculateDeltaVector(Vector2 newMousePos, Vector2 initialClickPos)
        {
            return newMousePos - initialClickPos;
        }

        private static Vector2 GetMousePosition()
        {
            return Input.mousePosition;
        }

        #endregion

        #region Layer Rotation

        // Predefined set of cube orientations considered 'inverted'
        // Used to determine correct rotation direction in certain views
        private readonly List<Vector3> _rotationInverses = new()
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 90, 0),

            new Vector3(0, 90, -180),
            new Vector3(0, 0, -180),

            new Vector3(90, -90, 0),
            new Vector3(0, -180, -90),

            new Vector3(0, -90, 90),
            new Vector3(-90, 0, 0),

            new Vector3(90, 90, 0),
            new Vector3(0, -180, 90),

            new Vector3(-90, -180, 0),
            new Vector3(0, -90, -90),
        };

        private static void UngroupPieces()
        {
            Instance.UngroupPieces();
        }

        /// <summary>
        /// Determine which face should rotate based on the piece clicked and drag direction.
        /// Groups the corresponding pieces for rotation.
        /// </summary>
        private void GroupPieces(Vector2 delta)
        {
            Transform piece = transform.parent;

            if (IsEdge(piece))
                _rotatingFaceIndex = GetEdgeFaceIndex(piece);
            else if (IsMiddleLayerSticker())
                _rotatingFaceIndex = GetMiddleFaceIndex(piece, delta);
            else
                _rotatingFaceIndex = GetUpDownFaceIndex(piece, delta);

            Instance.GroupPieces(_rotatingFaceIndex);
        }

        private static bool IsEdge(Transform piece) =>
            piece.name.Count(c => c == '0') == 1;

        private bool IsMiddleLayerSticker() =>
            transform.name is not "U" and not "D";

        private int GetEdgeFaceIndex(Transform piece)
        {
            Transform neighbour = piece.GetChild(transform.GetSiblingIndex() ^ 1);

            return FaceToIndex(neighbour.name);
        }

        private int GetMiddleFaceIndex(Transform piece, Vector2 delta)
        {
            if (_dragAxis.Equals(Y_AXIS * -1))
                return FaceToIndex(piece.GetChild(U_D_INDEX).name);

            Transform neighbour = piece.GetChild(L_R_INDEX) == transform
                ? piece.GetChild(B_F_INDEX)
                : piece.GetChild(L_R_INDEX);

            return FaceToIndex(neighbour.name);
        }

        private int GetUpDownFaceIndex(Transform piece, Vector2 delta)
        {
            Vector3 cubeRotation = CubeRotator.NormaliseVector(Instance.transform.eulerAngles);

            int sideIndex = delta.x * delta.y > 0 ^ RotationInversesContains(cubeRotation)
                ? L_R_INDEX
                : B_F_INDEX;

            return FaceToIndex(piece.GetChild(sideIndex).name);
        }

        /// <summary>
        /// True if 'euler' matches any entry in _rotationInverses within a small angular tolerance.
        /// Prevents floating-point drift from breaking orientation checks.
        /// </summary>
        private bool RotationInversesContains(Vector3 euler, float tolerance = SMALL_ANGLE_TOLERANCE)
        {
            return _rotationInverses.Any(v => Vector3.Distance(v, euler) < tolerance);
        }

        // Rounds each component to the nearest 90
        // The cube therefore remains in the same shape on the screen
        private static Vector3 QuantiseVector(Vector3 vector)
        {
            var v = new Vector3(
                Mathf.Round(vector.x / 90f),
                Mathf.Round(vector.y / 90f),
                Mathf.Round(vector.z / 90f)
            ) * 90f;

            return v;
        }

        private static Vector3 NormaliseVector(Vector3 vector)
        {
            var v = new Vector3(
                NormalizeAngle(vector.x),
                NormalizeAngle(vector.y),
                NormalizeAngle(vector.z)
            );

            return v;
        }

        private static float NormalizeAngle(float angle) => Mathf.Repeat(angle + 180f, 360f) - 180f;

        /// <summary>
        /// Returns the world direction vector of a given face.
        /// </summary>
        private static Vector3 GetRotationDirection(string face) =>
            face switch
            {
                "U" => Instance.transform.up,
                "D" => Instance.transform.up,
                "L" => Instance.transform.forward,
                "R" => Instance.transform.forward,
                "F" => -Instance.transform.right,
                "B" => -Instance.transform.right,
                _ => Vector3.zero
            };

        /// <summary>
        /// Returns the axis vector to rotate around for a given face.
        /// </summary>
        private static Vector3 GetRotationAxis(string face) =>
            face switch
            {
                "U" => Vector3.up,
                "D" => Vector3.up,
                "L" => Vector3.left,
                "R" => Vector3.left,
                "F" => Vector3.back,
                "B" => Vector3.back,
                _ => Vector3.zero
            };

        /// <summary>
        /// Calculates signed rotation magnitude from mouse movement.
        /// </summary>
        private float GetRotationMagnitude()
        {
            Vector2 delta = GetMousePosition() - _lastMousePos;

            // convert 2D drag to world space
            Vector3 screenDrag = new(delta.x, delta.y, 0f);
            Vector3 worldDrag = Camera.main.transform.TransformDirection(screenDrag);

            // normal face in world space
            Vector3 faceNormal = GetRotationDirection(ColourToFace(_rotatingFaceIndex));

            // get tangent in the face plane to calculate rotation direction
            // Cross() with camera forward to find a drag reference
            Vector3 faceRight = Vector3.Cross(faceNormal, Camera.main.transform.forward).normalized;

            // project drag direction onto this tangent
            float signedMagnitude = -Vector3.Dot(worldDrag, faceRight);

            return signedMagnitude;
        }

        /// <summary>
        /// Translates the quantised rotation into solver notation.
        /// </summary>
        private string GetMoveFromRotation()
        {
            var rotation = NormaliseVector(_quantisedRotation);

            // Reduce the 3-axis quantised rotation to a single "step count" in 90° units
            float signedValue = rotation.x + rotation.y + rotation.z - STARTING_ROTATON.magnitude; // account for the initial rotation of the cube
            signedValue = Mathf.Repeat(signedValue / 90f, 4f);

            if (signedValue == 0)
                return "";

            // Compose the move string
            return ColourToFace(_rotatingFaceIndex) + signedValue switch
            {
                1 => _rotatingFaceIndex is 1 or 5 ? "" : "'", // quarter turn; invert for non-U/D
                2 => "2",                                     // half turn
                3 => _rotatingFaceIndex is 1 or 5 ? "'" : "", // three-quarter (i.e. inverse quarter)
                _ => ""
            };
        }

        #endregion
    }
}