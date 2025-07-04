using System.Collections.Generic;
using System.Linq;
using Model;
using UnityEngine;
using Engine;
using static UI.Square;

namespace UI
{
    public class Cube : MonoBehaviour
    {
        public static Cube Instance;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(Instance);
        }

        [SerializeField] private Transform piecePrefab;
        [SerializeField] private float animationSpeed;

        private Transform _rotationPlane;

        private Cubie _cubie;

        private Facelet _facelet;


        private List<int> _movedPieceIndexes = new();

        private bool _initList;

        private List<Transform> _pieceTransformList;

        private void Update()
        {
            if (_initList)
            {
                _pieceTransformList = new();
                for (int i = 0; i < transform.childCount; i++)
                {
                    _pieceTransformList.Add(transform.GetChild(i));
                }

                _initList = false;

                UpdateModels();
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                System.Random r = new ();

                int total = r.Next(20, 40);

                for (int i = 0; i < total; i++)
                {
                    string move = ColourToFace(r.Next(0, 5));
                    if (_moveQueue.Count > 1)
                        if (move == _moveQueue[^1][0].ToString()) continue;

                    switch (r.Next(0, 10))
                    {
                        case 0:
                        case 2:
                        case 3:
                            move += "'";
                            break;
                        case 1:
                            move += "2";
                            break;
                        default:
                            break;
                    }
                    _moveQueue.Add(move);
                }
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                GenerateFacelet();
                Debug.Log(Validation.Validate(_facelet));
            }


            KeyCode[] moveInputs = { KeyCode.U, KeyCode.D, KeyCode.F, KeyCode.B, KeyCode.R, KeyCode.L };
            foreach (KeyCode key in moveInputs)
                if (Input.GetKeyDown(key))
                {
                    _moveQueue.Add(key.ToString() + (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? "'" : ""));
                    break; 
                }

            if (_moveQueue.Count == 0) return;

            if (!_isAnimating)
                Move();
            AnimateMove();
        }

        private void Start()
        {
            GenerateCube();
            InitRotationPlaneTransform();
        }

        private void GenerateCube()
        {
            // iterates through all 27 (3^3) piece positions
            for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
            for (int k = -1; k <= 1; k++)
            {
                var piece = Instantiate(piecePrefab, transform);
                piece.localPosition = new Vector3(i, j, k);
                piece.name = $"{i} {j} {k}";

                // removes inner faces
                if (k <= 0)
                    Destroy(piece.GetChild(0).gameObject);
                if (k >= 0)
                    Destroy(piece.GetChild(1).gameObject);
                if (j <= 0)
                    Destroy(piece.GetChild(2).gameObject);
                if (j >= 0)
                    Destroy(piece.GetChild(3).gameObject);
                if (i <= 0)
                    Destroy(piece.GetChild(4).gameObject);
                if (i >= 0)
                    Destroy(piece.GetChild(5).gameObject);
            }

            // Destroy() methods are called after each frame
            // Therefore will not be called during the Start() procedure
            // So initialise list on the next frame in Update()
            _initList = true;
        }

        private void InitRotationPlaneTransform()
        {
            _rotationPlane = Instance.transform.Find("Rotation Plane");
            _rotationPlane.parent = transform;
            _rotationPlane.SetAsLastSibling();
        }

        public void UpdateModels()
        {
            GenerateFacelet();
            ConvertFaceletToCubie();
        }

        private static readonly int[][] ColourPieceMap =
        {
            new[] { 18, 19, 20, 9 , 11, 0 , 1 , 2  }, // D
            new[] { 6 , 7 , 8 , 15, 17, 24, 25, 26 }, // U
            new[] { 24, 25, 26, 21, 23, 18, 19, 20 }, // B
            new[] { 8 , 7 , 6 , 5 , 3 , 2 , 1 , 0  }, // F
            new[] { 26, 17, 8 , 23, 5 , 20, 11, 2  }, // L
            new[] { 6 , 15, 24, 3 , 21, 0 , 9 , 18 }, // R
        };

        private static readonly int[][] ColourSquareMap =
        {
            new[] { 1, 0, 1, 1, 1, 1, 0, 1 }, // D
            new[] { 1, 0, 1, 1, 1, 1, 0, 1 }, // U
            new[] { 2, 1, 2, 1, 1, 2, 1, 2 }, // B
            new[] { 2, 1, 2, 1, 1, 2, 1, 2 }, // F
            new[] { 0, 0, 0, 0, 0, 0, 0, 0 }, // L
            new[] { 0, 0, 0, 0, 0, 0, 0, 0 }, // R
        };

        /// <summary>
        /// Takes the graphical cube and converts it to a facelet model.
        /// 
        /// Finds the pieces, on the graphical cube, that forms a face.
        /// Selects the correct square on each piece that forms the face.
        /// </summary>
        private void GenerateFacelet()
        {
            _facelet = new Facelet();
            // iterate through all 6 faces of the cube
            for (int i = 0; i <= 5; i++)
            {
                _facelet.Faces[i] = new List<int>();

                for (int j = 0; j < 8; j++)
                    AddToFacelet(_facelet, i, ColourPieceMap[i][j], ColourSquareMap[i][j]);
            }
        }

        /// <summary>
        /// Adds colour to a square on the facelet model
        /// </summary>
        /// <param name="cube">Cube to add square to</param>
        /// <param name="face">Face to add square to</param>
        /// <param name="pieceIndex">Index of the piece in a concatenated list of pieces</param>
        /// <param name="squareIndex">Index of the square on the piece</param>
        private void AddToFacelet(Facelet cube, int face, int pieceIndex, int squareIndex)
        {
            // finds the square in the scene
            // determine which colour it is
            int colour = _pieceTransformList[pieceIndex].GetChild(squareIndex).GetComponent<Square>().colour;
            cube.Faces[face].Add(colour);
        }

        private void SetColours(Facelet f)
        {
            for (int i = 0; i < 6; i++)
            for (int j = 0; j < 8; j++)
            {
                SetSquareColour(f.Faces[i][j], ColourPieceMap[i][j], ColourSquareMap[i][j]);
            }
        }

        private void SetSquareColour(int colour, int pieceIndex, int squareIndex)
        {
            var square = _pieceTransformList[pieceIndex].GetChild(squareIndex).GetComponent<Square>();
            square.colour = colour;
            square.UpdateColour();
        }

        private void ConvertFaceletToCubie()
        {
            _cubie = Converter.FaceletToCubie(_facelet, false);
        }

        # region Move Animation   
        
        private Quaternion _destinationOrientation = Quaternion.identity;
        private Quaternion _startOrientation = Quaternion.identity;

        private bool _isAnimating;

        private List<string> _moveQueue = new();

        // Indexes of centre pieces
        private readonly int[] _centrePieces = { 10, 16, 22, 4, 14, 12 };
        
        private void Move()
        {
            _startOrientation = _rotationPlane.localRotation;
            _destinationOrientation = Quaternion.Euler(_rotationPlane.localRotation.eulerAngles + GetRotation(_moveQueue[0]));
            _isAnimating = true;

            GroupPieces(Square.FaceToIndex(_moveQueue[0][0].ToString()));
        }

        private static Vector3 GetRotation(string move)
        {
            string face = move[0].ToString();

            var moveVector = face switch
            {
                "U" => Vector3.up,
                "R" => Vector3.right,
                "F" => Vector3.back,
                "D" => Vector3.down,
                "L" => Vector3.left,
                "B" => Vector3.back,
                _ => Vector3.zero,
            } * 90f;

            return move[^1] switch
            {
                '\'' => moveVector * -1,
                '2' => moveVector * 2,
                _ => moveVector
            };
        }

        private void AnimateMove()
        {
            _rotationPlane.localRotation = Quaternion.RotateTowards(_rotationPlane.localRotation, _destinationOrientation, animationSpeed * 100 * Time.deltaTime);

            if (_rotationPlane.localRotation != _destinationOrientation) return;

            _isAnimating = false;
            // Undo rotation ...
            _rotationPlane.localRotation = _startOrientation;
            UngroupPieces();

            // ... then do an instant rotation after the cubie rotation
            _cubie.Move(_moveQueue[0]);
            _facelet = Converter.CubieToFacelet(_cubie);
            SetColours(_facelet);

            _moveQueue.RemoveAt(0);
        }

        private void GroupPieces(int face)
        {
            _movedPieceIndexes = ColourPieceMap[face].ToList();
            _movedPieceIndexes.Add(_centrePieces[face]);
            // When ungroup pieces, need to place pieces in the correct order 
            // Indexes will be messed up if not in order.
            _movedPieceIndexes.Sort();

            foreach (var pieceOnFaceIndex in _movedPieceIndexes)
            {
                _pieceTransformList[pieceOnFaceIndex].SetParent(_rotationPlane);
            }
        }

        private void UngroupPieces()
        {
            int count = _rotationPlane.childCount;
            for (int i = 0; i < count; i++)
            {
                _rotationPlane.GetChild(0).SetParent(transform);
                transform.GetChild(transform.childCount - 1).SetSiblingIndex(_movedPieceIndexes[i]);
            }
        }

        #endregion
    }
}
