using UnityEngine;
using static System.MathF;

public class Orientator : MonoBehaviour
{
    public Transform cube;
    public Camera cam;

    private bool isOrientating = false;
    private bool isHovering = false;
    private bool isCalculated = false;
    private bool isSnapping = false;

    private Vector2 initialClickPos = Vector2.zero;
    private Vector2 lastMousePos = Vector2.zero;

    private Vector3 dragAxis;

    private Quaternion quantisedRotation;
    
    private void OnMouseEnter()
    {
        isHovering = true;
    }
    private void OnMouseExit()
    {
        isHovering = false;
    }

    private void Update()
    {
        HandleRMBRelease();

        HandleRotationSnapping();

        HandleRMBPress();

        if (!isOrientating)
            return;

        HandleMouseMove();
    }

    private void HandleRotationSnapping()
    {
        if (!isSnapping) return;

        cube.rotation = Quaternion.RotateTowards(cube.rotation, quantisedRotation, 100 * Time.deltaTime);

        if (cube.rotation.eulerAngles == quantisedRotation.eulerAngles)
            isSnapping = false;
    }

    private void HandleRMBRelease()
    {
        if (Input.GetMouseButtonUp(1) && isCalculated)
        {
            isOrientating = isCalculated = false;

            quantisedRotation = Quaternion.Euler(QuantiseVector(cube.eulerAngles));
            isSnapping = true;
        }
    }

    private void HandleRMBPress()
    {
        if (Input.GetMouseButtonDown(1) && isHovering && !isCalculated && !isOrientating)
        {
            lastMousePos = initialClickPos = Input.mousePosition - new Vector3(Screen.width / 2f, Screen.height / 2f);
            isOrientating = true;
        }
    }

    private void HandleMouseMove()
    {
        Vector2 newMousePos = Input.mousePosition - new Vector3(Screen.width / 2f, Screen.height / 2f);

        if (newMousePos != initialClickPos)
        {

            if (!isCalculated)
                CalculateDragDirection(newMousePos, initialClickPos);

            if (!isCalculated) return;

            float rotation = dragAxis.y == 0 ? newMousePos.y - lastMousePos.y : (newMousePos.x - lastMousePos.x) / 2f;

            rotation /= Time.deltaTime * 400f;

            cube.Rotate(dragAxis, rotation, Space.World);

            lastMousePos = newMousePos;
        }
    }

    private void CalculateDragDirection(Vector2 newMousePos, Vector2 initialClickPos)
    {
        var delta = CalculateDeltaVector(newMousePos, initialClickPos);

        if (delta.magnitude < 10f) return;

        var modDelta = CalculateModulusVector(delta);

        if (modDelta.x > modDelta.y)
        {
            dragAxis = new Vector3(0, 1, 0) * -1;
        }
        else
        {
            bool isLeftSide = initialClickPos.x < 0;

            dragAxis = isLeftSide ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1);
        }

        isCalculated = true;

    }

    private Vector2 CalculateModulusVector(Vector2 delta)
    {
        return new Vector2(Abs(delta.x), Abs(delta.y));
    }

    private Vector2 CalculateDeltaVector(Vector2 newMousePos, Vector2 initialClickPos)
    {
        return newMousePos - initialClickPos;
    }

    private Vector3 QuantiseVector(Vector3 vector)
    {
        return new Vector3(
            Mathf.Round(vector.x / 90f),
            Mathf.Round(vector.y / 90f),
            Mathf.Round(vector.z / 90f)
        ) * 90f;
    }

}
