using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using static Model.Converter;
using UnityEngine;
using Engine;
using static UI.Square;
using UnityEngine.UI;

namespace UI
{
    public class Cube : MonoBehaviour
    {
        // singleton instance
        public static Cube Instance;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(Instance);
        }

        /* === SERIALIZED === */
        [SerializeField] private Transform piecePrefab;

        /* === PUBLIC === */
        public float animationSpeed = 10;
        [HideInInspector] public bool isOrientating;

        /* === PRIVATE === */

        // core components
        private Transform _rotationPlane;
        private Cubie _cubie;
        private Facelet _facelet;
        private Facelet _startingFacelet;

        // state management
        private bool _initList;
        private bool _newSolution;
        private bool _isSolving;

        // lists
        private List<int> _movedPieceIndexes = new();
        private List<Transform> _pieceTransformList;

        // input
        private readonly KeyCode[] _moveInputs =
        {
            KeyCode.U, KeyCode.D, KeyCode.F,
            KeyCode.B, KeyCode.R, KeyCode.L
        };

        // solver
        private Solver _solver;

        private void Start()
        {
            GenerateCube();

            InitRotationPlaneTransform();

            InitSolver();
        }

        private void Update()
        {
            InitPieceTransformList();

            HandleMoveInputs();

            HandleZoom();

            HandleMoves();

            HandleNewMoveList();
        }

        #region Cube Graphics

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
                if (k <= 0) Destroy(piece.GetChild(0).gameObject);
                if (k >= 0) Destroy(piece.GetChild(1).gameObject);
                if (j <= 0) Destroy(piece.GetChild(2).gameObject);
                if (j >= 0) Destroy(piece.GetChild(3).gameObject);
                if (i <= 0) Destroy(piece.GetChild(4).gameObject);
                if (i >= 0) Destroy(piece.GetChild(5).gameObject);
            }

            // Destroy() methods are called after each frame
            // Therefore will not be called during the Start() procedure
            // So initialise list on the next frame in Update()
            _initList = true;
        }
        
        public void ResetColours()
        {
            SetCubie(new Cubie(Cubie.Identity));
            UpdateFacelet();

            SetColours(_facelet);
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
        private void GenerateFaceletFromGraphics()
        {
            _facelet = new Facelet();

            // Iterate through all 6 faces of the cube
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                _facelet.Faces[faceIndex] = new List<int>();

                for (int squareIndex = 0; squareIndex < 8; squareIndex++)
                    AddGraphicsToFacelet(
                        _facelet, 
                        faceIndex, 
                        ColourPieceMap[faceIndex][squareIndex], 
                        ColourSquareMap[faceIndex][squareIndex]
                    );
            }
        }

        public void SetColours(Facelet f)
        {
            for (int i = 0; i < 6; i++)
            for (int j = 0; j < 8; j++)
                SetSquareColour(f.Faces[i][j], ColourPieceMap[i][j], ColourSquareMap[i][j]);
        }

        private void SetSquareColour(int colour, int pieceIndex, int squareIndex)
        {
            var square = _pieceTransformList[pieceIndex].GetChild(squareIndex).GetComponent<Square>();
            square.colour = colour;
            square.UpdateGraphics();
        }

        /// <summary>
        /// Adds colour to a square on the facelet model
        /// </summary>
        /// <param name="cube">Cube to add square to</param>
        /// <param name="face">Face to add square to</param>
        /// <param name="pieceIndex">Index of the piece in a concatenated list of pieces</param>
        /// <param name="squareIndex">Index of the square on the piece</param>
        private void AddGraphicsToFacelet(Facelet cube, int face, int pieceIndex, int squareIndex)
        {
            // finds the square in the scene
            // determine which colour it is
            int colour = _pieceTransformList[pieceIndex].GetChild(squareIndex).GetComponent<Square>().colour;
            cube.Faces[face].Add(colour);
        }

        #endregion

        #region Initialisers

        private void InitSolver()
        {
            _solver = new Solver();
        }

        private void InitRotationPlaneTransform()
        {
            _rotationPlane = Instance.transform.Find("Rotation Plane");
            _rotationPlane.parent = transform;
            _rotationPlane.SetAsLastSibling();
        }

        private void InitPieceTransformList()
        {
            if (!_initList) return;

            _pieceTransformList = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
                _pieceTransformList.Add(transform.GetChild(i));

            _initList = false;

            UpdateModelsFromGraphics();
        }

        #endregion

        #region Model Management

        public void SetFacelet(Facelet facelet) => _facelet = facelet;

        public void SetCubie(Cubie cubie) => _cubie = cubie;

        public void UpdateCubie() => _cubie = FaceletToCubie(_facelet, false);

        public void UpdateFacelet() => _facelet = CubieToFacelet(_cubie);

        public void UpdateModelsFromGraphics()
        {
            GenerateFaceletFromGraphics();
            UpdateCubie();
        }

        #endregion

        #region Input Events

        public async void Solve(int stage)
        {
            GenerateFaceletFromGraphics();
            if (Validation.Validate(_facelet))
            {
                Manager.Instance.SwitchInterface();
                _startingFacelet = new Facelet(_facelet);
                ClearQueue();
                await _solver.SolveAsync(FaceletToCubie(_facelet), stage);
                Manager.Instance.ToggleInvalidNotification(false);
            }
            else
                Manager.Instance.ToggleInvalidNotification(true);
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (!(Mathf.Abs(scroll) > 0.01f)) return;
            
            // get current scale
            Vector3 currentScale = transform.localScale;

            // calculate new scale
            Vector3 newScale = currentScale + Vector3.one * scroll;

            // clamp scale within limits
            float clampedScale = Mathf.Clamp(newScale.x, 0.1f, 1.3f);
            transform.localScale = new Vector3(clampedScale, clampedScale, clampedScale);
        }

        public void Shuffle()
        {
            System.Random r = new();

            int total = r.Next(60, 80);

            for (int i = 0; i < total; i++)
            {
                string move = ColourToFace(r.Next(0, 6));
                if (_moveQueue.Count > 0 && move == _moveQueue.Last()[0].ToString()) continue;

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
                }

                Enqueue(move);
            }
        }

        private void HandleMoveInputs()
        {
            foreach (KeyCode key in _moveInputs)
            {
                if (Input.GetKeyDown(key))
                {
                    bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

                    string move = key.ToString();
                    if (shift) move += "'";
                    if (alt) move += "2";

                    Enqueue(move);
                    break;
                }
            }
        }

        #endregion

        #region Solution

        public void Enqueue(string move) => _moveQueue.Add(move);

        public void EnqueueSolution(Queue<string> moves)
        {
            while (moves.Count > 0)
                _moveQueue.Add(moves.Dequeue());
            _newSolution = true;
        }

        public List<string> GetSolution() => _solver.Moves;

        #endregion

        #region Move Animation   

        private Quaternion _destinationOrientation = Quaternion.identity;
        private Quaternion _startOrientation = Quaternion.identity;

        private bool _isAnimating;

        private readonly List<string> _moveQueue = new();
        private int _currentIndex = 0;
        private string _currentMove;

        // indexes of centrepieces
        private readonly int[] _centrePieces = { 10, 16, 22, 4, 14, 12 };

        public void ClearQueue()
        {
            _moveQueue.Clear();
            _currentIndex = 0;
        }

        private void Move()
        {
            BeginMove(_moveQueue[_currentIndex]);
            _currentIndex++;
            Manager.Instance.SwitchMoveText();
        }

        private void Unmove()
        {
            _currentIndex--;
            string move = ReverseMove(_moveQueue[_currentIndex]);
            BeginMove(move);
            Manager.Instance.SwitchMoveText(false);
        }

        private void BeginMove(string move)
        {
            _startOrientation = _rotationPlane.localRotation;
            _currentMove = move;
            _destinationOrientation = Quaternion.Euler(
                _rotationPlane.localRotation.eulerAngles + GetRotation(move));
            _isAnimating = true;

            GroupPieces(FaceToIndex(move[0].ToString()));
        }

        private string ReverseMove(string move)
        {
            char face = move[0];
            bool isDouble = move.Contains('2');
            bool isPrime = move.Contains('\'');
            
            // U2 -> U'2
            if (isDouble) return $"{face}'2";
            // U' -> U
            else if (isPrime) return face.ToString();
            // U -> U'
            else return $"{face}'";
        }

        private static Vector3 GetRotation(string move)
        {
            char face = move[0];
            bool isPrime = move.Contains('\'');
            bool isDouble = move.Contains('2');

            Vector3 axis = face switch
            {
                'U' => Vector3.up,
                'D' => Vector3.down,
                'L' => Vector3.left,
                'R' => Vector3.right,
                'F' => Vector3.back,
                'B' => Vector3.back,
                _ => Vector3.zero
            };

            float angle = 90f;

            if (isDouble)
                angle *= 2;

            if (isPrime)
                angle *= -1;

            if (isDouble && face is 'F' or 'R' or 'L')
                angle *= -1;

            return axis * angle;
        }

        private void AnimateMove()
        {
            _rotationPlane.localRotation = Quaternion.RotateTowards(
                _rotationPlane.localRotation,
                _destinationOrientation,
                animationSpeed * 100 * Time.deltaTime);

            if (_rotationPlane.localRotation != _destinationOrientation) return;

            // Undo rotation ...
            _rotationPlane.localRotation = _startOrientation;
            UngroupPieces();

            // ... then do an instant rotation after the cubie rotation
            _cubie.Move(_currentMove);
            UpdateFacelet();
            SetColours(_facelet);

            _isAnimating = false;
        }

        private void GroupPieces(int face)
        {
            _movedPieceIndexes = ColourPieceMap[face].ToList();
            _movedPieceIndexes.Add(_centrePieces[face]);
            // When ungroup pieces, need to place pieces in the correct order 
            // Indexes will be messed up if not in order.
            _movedPieceIndexes.Sort();

            foreach (var pieceOnFaceIndex in _movedPieceIndexes)
                _pieceTransformList[pieceOnFaceIndex].SetParent(_rotationPlane);
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

        public void UpdateAnimationSpeed(Slider slider)
        {
            animationSpeed = slider.value;
        }

        #endregion

        #region Move Controls

        private bool _isAutomating;
        
        private void HandleMoves()
        {
            if (_isAnimating && !isOrientating)
                AnimateMove();

            if (!_isAutomating && _isSolving)
                return;

            if (_currentIndex < _moveQueue.Count && !_isAnimating)
                Move();
        }

        private enum MoveCommand
        {
            Forward = 0,
            Backward = 1,
            ToEnd = 2,
            ToStart = 3,
            ToggleAuto = 4
        }
        
        public void HandleMoveControls(int command)
        {
            bool isBusy = _isAnimating || _isAutomating;

            switch ((MoveCommand)command)
            {
                case MoveCommand.Forward:
                    if (isBusy || !CanMoveForward()) return;
                    
                    Move();
                    break;

                case MoveCommand.Backward:
                    if (isBusy || !CanMoveBackward()) return;
                    
                    Unmove();
                    break;

                case MoveCommand.ToEnd:
                    if (isBusy) return;

                    SetToEnd();
                    break;

                case MoveCommand.ToStart:
                    if (isBusy) return;
                    
                    SetToStart();
                    break;

                case MoveCommand.ToggleAuto:
                    ToggleAutomation();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command, null);
            }
        }

        private bool CanMoveForward() => _currentIndex < _moveQueue.Count;

        private bool CanMoveBackward() => _currentIndex > 0;

        private void SetToEnd()
        {
            SetCubie(Cubie.Identity);
            UpdateFacelet();
            SetColours(_facelet);

            _currentIndex = _moveQueue.Count;

            Manager.Instance.ListToEnd();
        }

        private void SetToStart()
        {
            SetFacelet(_startingFacelet);
            UpdateCubie();
            SetColours(_facelet);

            _currentIndex = 0;

            Manager.Instance.ListToStart();
        }

        private void ToggleAutomation() => _isAutomating = !_isAutomating;

        #endregion
        
        private void HandleNewMoveList()
        {
            if (_newSolution)
            {
                Manager.Instance.UpdateMoveList();
                _newSolution = false;
            }
        }

        public void SetIsSolving(bool b)
        {
            _isSolving = b;
        }
    }
}
