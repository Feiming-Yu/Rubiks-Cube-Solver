using Model;
using System;
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

/////////////////////////////////////////////////////////////////////////////////////////////////////
        // ill be so real rn, i really can't be asked to deal with this complicated shit
        // i dont even need this in the app anyways
/////////////////////////////////////////////////////////////////////////////////////////////////////

        ////#region Mouse Direciton

        ////private bool _isRotating;
        ////private bool _isHovering;
        ////private bool _isCalculated;
        ////private bool _isSnapping;

        ////private Vector2 _initialClickPos = Vector2.zero;
        ////private Vector2 _lastMousePos = Vector2.zero;
        ////private Vector2 dragDirection = Vector2.zero;

        ////private Vector3 _dragAxis;

        ////private Quaternion _quantisedRotation;

        ////private void OnMouseEnter()
        ////{
        ////    _isHovering = true;
        ////}

        ////private void OnMouseExit()
        ////{
        ////    _isHovering = false;
        ////}

        ////void Update()
        ////{
        ////    HandleRMBRelease();

        ////    HandleRotationSnapping();

        ////    HandleRMBPress();

        ////    if (_isRotating)
        ////        HandleMouseMove();
        ////}

        ////private void HandleRMBRelease()
        ////{
        ////    if (!Input.GetMouseButtonUp(1) || !_isCalculated) return;

        ////    _isRotating = _isCalculated = false;

        ////    _quantisedRotation = Quaternion.Euler(QuantiseVector(_rotationPlane.eulerAngles));
        ////    //
        ////    //// Start the animation to snap the cube to a quantised rotation
        ////    //_isSnapping = true;

        ////    _isSnapping = true;
        ////}

        ////private void HandleRMBPress()
        ////{
        ////    if (!Input.GetMouseButtonDown(1) || !_isHovering || _isCalculated || _isRotating) return;

        ////    _lastMousePos = _initialClickPos = GetMousePosition();

        ////    _isRotating = true;
        ////}

        ////private void HandleMouseMove()
        ////{
        ////    Vector2 newMousePos = GetMousePosition();

        ////    // Calculation only begins if the mouse has moved
        ////    if (newMousePos == _initialClickPos) return;
        ////    // Locks the direction of the drag until button is released
        ////    if (!_isCalculated)
        ////        CalculateDragDirection(newMousePos, _initialClickPos);

        ////    // Checks if the change in mouse position is significant
        ////    if (!_isCalculated) return;

        ////    float rotation = _dragAxis.y == 0 ? newMousePos.y - _lastMousePos.y : (newMousePos.x - _lastMousePos.x) / 2f;
        ////    rotation /= Time.deltaTime * 400f;

        ////    RotateLayer(rotation);

        ////    _lastMousePos = newMousePos;
        ////}


        ////private void CalculateDragDirection(Vector2 newMousePos, Vector2 initialClickPos)
        ////{
        ////    Vector2 delta = CalculateDeltaVector(newMousePos, initialClickPos);

        ////    // Checks if the vector change is significant
        ////    // True if beyond the threshold of 10 pixels
        ////    // Reduces noise and accidental input
        ////    if (delta.magnitude < 10f) return;

        ////    var modDelta = CalculateModulusVector(delta);


        ////    // If the change in x-position is more significant
        ////    if (modDelta.x > modDelta.y)
        ////        // Multiply by -1 as direction of rotation is opposite to mouse movement
        ////        _dragAxis = new Vector2(0, 1) * -1;
        ////    else
        ////        _dragAxis = new Vector2(1, 0);

        ////    var modulusDragDirection = CalculateModulusVector(_dragAxis);
        ////    Vector2 rotationAxis = new (modulusDragDirection.y, modulusDragDirection.x);
        ////    dragDirection = new Vector2(delta.x / modDelta.x, delta.y / modDelta.y) * rotationAxis;

        ////    // Locks the drag direction to avoid a two-dimensional rotation value
        ////    _isCalculated = true;
        ////}

        /////// <summary>
        /////// Applies the modulus function on the x and y component.
        /////// </summary>
        /////// <param name="v"></param>
        /////// <returns></returns>
        ////private Vector2 CalculateModulusVector(Vector2 v)
        ////{
        ////    return new Vector2(Abs(v.x), Abs(v.y));
        ////}

        ////private Vector2 CalculateDeltaVector(Vector2 newMousePos, Vector2 initialClickPos)
        ////{
        ////    return newMousePos - initialClickPos;
        ////}

        ////private Vector2 GetMousePosition()
        ////{
        ////    return Input.mousePosition;
        ////}

        ////#endregion

        //#region Layer Rotation

        //private Transform _rotationPlane;

        //private Quaternion _originalRotation;
        //private Quaternion _quantisedRotation;

        //private List<int> _movedPieceIndexes = new();

        //private bool isPlaneCalculated; 
        //private bool _isSnapping;

        //private int _rotatingFace;

        //private void Start()
        //{
        //    _rotationPlane = Instance.transform.parent.Find("Rotation Plane");
        //}

        //private readonly int[] centrePieces = new int[6]
        //{
        //    10, 16, 22, 4, 14, 12
        //};

        ////private void RotateLayer(float rotation)
        ////{

        ////    if (!isPlaneCalculated)
        ////    {
        ////        _originalRotation = _rotationPlane.rotation;

        ////        int pieceIndex = transform.parent.GetSiblingIndex();
        ////        int selectedFace = FaceToIndex(name);

        ////        List<int> rotatableFaces = GetPieceSides(pieceIndex);

        ////        if (!rotatableFaces.Remove(selectedFace))
        ////            throw new Exception("Unexpected error: selected face not present of the piece's faces");

        ////        _rotatingFace = rotatableFaces[(int) Abs(_dragAxis.x)];

        ////        CalculateRotationProperties(_rotatingFace, selectedFace);

        ////        GroupPieces();

        ////        isPlaneCalculated = true;

        ////    }

        ////    _rotationPlane.Rotate(_dragAxis, rotation, Space.World);

        ////}

        //private void CalculateRotationProperties(int rotatingFace, int selectedFace)
        //{
        //    if (rotatingFace == 2 || rotatingFace == 3)
        //        if (selectedFace == 0 || selectedFace == 1)
        //            _dragAxis = new Vector3(0, -_dragAxis.x, -_dragAxis.y);
        //        else
        //            _dragAxis = new Vector3(0, _dragAxis.y, _dragAxis.x);
        //}

        //private void GroupPieces()
        //{

        //    _movedPieceIndexes = ColourPiecePositions[_rotatingFace].ToList();
        //    _movedPieceIndexes.Add(centrePieces[_rotatingFace]);
        //    _movedPieceIndexes.Sort();

        //    foreach (var pieceOnFaceIndex in _movedPieceIndexes)
        //    {
        //        Instance.PieceTransformList[pieceOnFaceIndex].SetParent(_rotationPlane);
        //    }
        //}
        //}
        //private void UngroupPieces()
        //{
        //    int count = _rotationPlane.childCount;
        //    for (int i = 0; i < count; i++)
        //    {
        //        _rotationPlane.GetChild(0).SetParent(Instance.transform);
        //        Instance.transform.GetChild(Instance.transform.childCount - 1).SetSiblingIndex(_movedPieceIndexes[i]);
        //    }


        //private void HandleRotationSnapping()
        //{
        //    if (!_isSnapping) return;

        //    // Animates the cube from its current rotation to the quantised rotation
        //    // 100 used as a speed constant. Higher means faster animation
        //    _rotationPlane.rotation = Quaternion.RotateTowards(_rotationPlane.rotation, _quantisedRotation, 200 * Time.deltaTime);

        //    // Stop animation once the current cube has reached quantised rotation
        //    if (_rotationPlane.rotation.eulerAngles == _quantisedRotation.eulerAngles)
        //    {
        //        Cube.Instance.Move(move);
        //        _rotationPlane.rotation = _originalRotation;

        //        _isSnapping = false;

        //        UngroupPieces();
        //        isPlaneCalculated = false;
        //    }
        //}

        ////private string GetMove()
        ////{
        ////    string move = ColourToFace(_rotatingFace);
        ////    bool isNegativeDrag = dragDirection.x + dragDirection.y < 0;

        ////    bool isNegative = isNegativeDrag ^ !invert;
        ////    bool isNonDominant = _rotatingFace % 2 == 0;

        ////    Debug.Log(dragDirection + " " + isNegative + " " + isNonDominant);

        ////    bool prime = isNegative ^ isNonDominant;

        ////    var quantisedOrientationDelta = Quantise(_originalRotation.eulerAngles.magnitude - _rotationPlane.rotation.eulerAngles.magnitude);
        ////    bool isDouble = quantisedOrientationDelta % 180 == 0;

        ////    if (isDouble)
        ////        return move + "2";

        ////    if (prime)
        ////        return move + "'";

        ////    return move;

        ////}

        //private List<int> GetPieceSides(int index)
        //{
        //    List<int> sides = new List<int>();
        //    for (int i = 0; i < 6; i++)
        //    {
        //        if (ColourPiecePositions[i].Contains(index))
        //            sides.Add(i);
        //    }

        //    return sides;
        //}

        //// Rounds each component to the nearest 90
        //// The cube therefore remains in the same shape on the screen
        //private Vector3 QuantiseVector(Vector3 vector)
        //{
        //    return new Vector3(
        //        Mathf.Round(vector.x / 90f),
        //        Mathf.Round(vector.y / 90f),
        //        Mathf.Round(vector.z / 90f)
        //    ) * 90f;
        //}

        //private float Quantise(float x)
        //{
        //    return Mathf.Round(x / 90f) * 90f;
        //}

        //#endregion
    }
}
