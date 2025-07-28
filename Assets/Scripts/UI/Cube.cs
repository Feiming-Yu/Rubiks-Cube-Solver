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

        [SerializeField] private Transform piecePrefab;

        public float animationSpeed = 10;
        [HideInInspector] public bool isOrientating;

        // core components
        private Transform _rotationPlane;
        private Cubie _cubie;
        private Facelet _facelet;
        private Facelet _startingFacelet;

        // state management
        private bool _initList;    // Flag to trigger initialization of piece transform list
        private bool _newSolution; // Indicates a new solution has been loaded
        private bool _isSolving;   // Whether the solver is currently solving
        private bool _isReset;     // Whether the cube is currently resetting colors

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

        private List<Facelet> _previousFacelets = new();

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

            HandleKeyboardShortcuts();

            HandleZoom();

            HandleMoves();

            HandleNewMoveList();
        }

        #region Cube Graphics

        private void GenerateCube()
        {
            // Iterates through all 27 (3^3) piece positions
            for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
            for (int k = -1; k <= 1; k++)
            {
                var piece = Instantiate(piecePrefab, transform);
                piece.localPosition = new Vector3(i, j, k);
                piece.name = $"{i} {j} {k}";

                // Removes inner faces
                if (k <= 0) Destroy(piece.GetChild(0).gameObject);
                if (k >= 0) Destroy(piece.GetChild(1).gameObject);
                if (j <= 0) Destroy(piece.GetChild(2).gameObject);
                if (j >= 0) Destroy(piece.GetChild(3).gameObject);
                if (i <= 0) Destroy(piece.GetChild(4).gameObject);
                if (i >= 0) Destroy(piece.GetChild(5).gameObject);
            }
            
            // Destroy() calls happen after the frame
            // so flag to initialize list next frame
            _initList = true;
        }
        
        public void ResetColours()
        {
            if (_moveQueue.Count != _currentIndex || _isAnimating)
                return;
                
            _moveQueue.Clear();
            _isReset = true;
        }

        private void HandleResetColours()
        {
            // Reset the logical cube to solved state
            SetCubie(Cubie.Identity);
            UpdateFacelet();

            // Update the colors on the graphical model
            SetColours(_facelet);
            _isReset = false;
        }

        // Maps face indices to the indexes of the pieces on that face
        private static readonly int[][] ColourPieceMap =
        {
            new[] { 18, 19, 20, 9 , 11, 0 , 1 , 2  }, // Down face
            new[] { 6 , 7 , 8 , 15, 17, 24, 25, 26 }, // Up face
            new[] { 24, 25, 26, 21, 23, 18, 19, 20 }, // Back face
            new[] { 8 , 7 , 6 , 5 , 3 , 2 , 1 , 0  }, // Front face
            new[] { 26, 17, 8 , 23, 5 , 20, 11, 2  }, // Left face
            new[] { 6 , 15, 24, 3 , 21, 0 , 9 , 18 }, // Right face
        };

        // Maps face indices to the sticker indexes on each piece
        private static readonly int[][] ColourSquareMap =
        {
            new[] { 1, 0, 1, 1, 1, 1, 0, 1 }, // Down face stickers
            new[] { 1, 0, 1, 1, 1, 1, 0, 1 }, // Up face stickers
            new[] { 2, 1, 2, 1, 1, 2, 1, 2 }, // Back face stickers
            new[] { 2, 1, 2, 1, 1, 2, 1, 2 }, // Front face stickers
            new[] { 0, 0, 0, 0, 0, 0, 0, 0 }, // Left face stickers
            new[] { 0, 0, 0, 0, 0, 0, 0, 0 }, // Right face stickers
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

                // For each sticker/square on the face, add its color to the facelet
                for (int squareIndex = 0; squareIndex < 8; squareIndex++)
                    AddGraphicsToFacelet(
                        _facelet, 
                        faceIndex, 
                        ColourPieceMap[faceIndex][squareIndex], 
                        ColourSquareMap[faceIndex][squareIndex]
                    );
            }
        }
        
        /// <summary>
        /// Updates the color of all squares on the graphical cube based on a Facelet model.
        /// </summary>
        public void SetColours(Facelet f)
        {
            for (int i = 0; i < 6; i++)
            for (int j = 0; j < 8; j++)
                SetSquareColour(f.Faces[i][j], ColourPieceMap[i][j], ColourSquareMap[i][j]);
        }
        
        /// <summary>
        /// Sets the color of a specific square (sticker) on a piece in the graphical cube.
        /// </summary>
        private void SetSquareColour(int colour, int pieceIndex, int squareIndex)
        {
            var square = _pieceTransformList[pieceIndex].GetChild(squareIndex).GetComponent<Square>();
            square.colour = colour;
            square.UpdateGraphics();
        }

        /// <summary>
        /// Adds the color of a specific square from the graphical cube to the facelet model.
        /// </summary>
        /// <param name="cube">Cube to add square to</param>
        /// <param name="face">Face to add square to</param>
        /// <param name="pieceIndex">Index of the piece in a concatenated list of pieces</param>
        /// <param name="squareIndex">Index of the square on the piece</param>
        private void AddGraphicsToFacelet(Facelet cube, int face, int pieceIndex, int squareIndex)
        {
            // Get the color from the graphical square
            int colour = _pieceTransformList[pieceIndex].GetChild(squareIndex).GetComponent<Square>().colour;
            cube.Faces[face].Add(colour);
        }

        public void UndoEdit()
        {
            if (_previousFacelets.Count == 0 || _currentIndex < _moveQueue.Count || _isAnimating) return;

            SetFacelet(_previousFacelets[^1]);
            UpdateCubie();
            SetColours(_previousFacelets[^1]);
            _previousFacelets.RemoveAt(_previousFacelets.Count - 1);
        }

        public void TrackCube()
        {
            _previousFacelets.Add(new Facelet(_facelet));
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

        public void SetFacelet(Facelet facelet) => _facelet = new Facelet(facelet);

        public Facelet GetFacelet() => _facelet;

        public void SetCubie(Cubie cubie) => _cubie = new Cubie(cubie);

        public void UpdateCubie() => _cubie = FaceletToCubie(_facelet, false);

        public void UpdateFacelet() => _facelet = CubieToFacelet(_cubie);

        public void UpdateModelsFromGraphics()
        {
            GenerateFaceletFromGraphics();
            UpdateCubie();
        }

        #endregion

        #region Input Events

        /// <summary>
        /// Begins the solving process asynchronously at a given stage.
        /// Validates the cube before solving.
        /// </summary>
        public async void Solve(int stage)
        {
            if (_moveQueue.Count != _currentIndex || _isAnimating)
                return;

            GenerateFaceletFromGraphics();
            if (Validation.Validate(_facelet))
            {
                ClearQueue();

                _previousFacelets.Clear();

                Manager.Instance.SwitchInterface();

                _startingFacelet = new Facelet(_facelet);


                await _solver.SolveAsync(FaceletToCubie(_facelet), stage);

                CubeErrorBox.Instance.Hide();
            }
            else
                CubeErrorBox.Instance.Show();
        }

        private void HandleZoom()
        {
            if (Manager.Instance.isWindowOpen) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");

            // Ignore insignificant scroll inputs
            if (!(Mathf.Abs(scroll) > 0.01f)) return;
            
            // Calculate new scale based on scroll input
            Vector3 currentScale = transform.localScale;
            Vector3 newScale = currentScale + Vector3.one * scroll;

            // Clamp scale within limits
            float clampedScale = Mathf.Clamp(newScale.x, 0.1f, 1.3f);
            transform.localScale = new Vector3(clampedScale, clampedScale, clampedScale);
        }

        public void Scramble()
        {
            if (_currentIndex < _moveQueue.Count || _isAnimating) return;

            TrackCube();

            System.Random r = new();

            int total = r.Next(60, 80);

            for (int i = 0; i < total; i++)
            {
                string move = ColourToFace(r.Next(0, 6));
                
                // Prevent repeated face moves consecutively
                if (_moveQueue.Count > 0 && move == _moveQueue.Last()[0].ToString()) continue;

                // Randomly add modifiers (', 2, or none)
                move += r.Next(0, 10) is <= 2 ? "'" : r.Next(0, 10) == 3 ? "2" : "";

                Enqueue(move);
            }
        }

        private void HandleMoveInputs()
        {
            if (_isSolving || Manager.Instance.isWindowOpen)
                return;

            foreach (KeyCode key in _moveInputs)
            {
                if (Input.GetKeyDown(key))
                {
                    TrackCube();

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
        private bool isProgressing = true;

        // Indexes of centrepieces
        private readonly int[] _centrePieces = { 10, 16, 22, 4, 14, 12 };

        public void ClearQueue()
        {
            _moveQueue.Clear();
            _currentIndex = 0;
        }

        private void Move()
        {
            isProgressing = true;
            BeginMove(_moveQueue[_currentIndex]);
            _currentIndex++;
        }

        private void Unmove()
        {
            isProgressing = false;
            _currentIndex--;
            string move = ReverseMove(_moveQueue[_currentIndex]);
            BeginMove(move);
            Manager.Instance.SwitchMoveText(false);
        }

        private void BeginMove(string move)
        {
            _startOrientation = _rotationPlane.localRotation;
            _currentMove = move;
            _destinationOrientation = Quaternion.Euler(_rotationPlane.localRotation.eulerAngles + GetRotation(move));
            _isAnimating = true;

            GroupPieces(FaceToIndex(move[0].ToString()));
        }

        private static string ReverseMove(string move)
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

            // Keep rotating until reached
            if (_rotationPlane.localRotation != _destinationOrientation) return;

            // Undo rotation ...
            _rotationPlane.localRotation = _startOrientation;
            UngroupPieces();

            // ... then do an instant rotation after the cubie rotation
            _cubie.Move(_currentMove);
            UpdateFacelet();
            SetColours(_facelet);

            if (isProgressing)
                Manager.Instance.SwitchMoveText(true);

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
            animationSpeed = slider.value == 6f ? 30f : slider.value;
        }

        #endregion

        #region Move Controls

        private bool _isAutomating;
        
        private void HandleMoves()
        {
            if (Manager.Instance.isWindowOpen)
                return;

            // Only animate if currently animating and if the cube is not rotating
            if (_isAnimating && !isOrientating)
                AnimateMove();

            // Only auto move if automating or in the editor for shuffling
            // Or only if the previous animation has finished
            if ((!_isAutomating && _isSolving) || _isAnimating)
                return;

            // Reset
            if (_isReset)
                HandleResetColours();
            // Auto move if more moves left
            else if (_currentIndex < _moveQueue.Count)
                Move();
            // Automation finished all the moves
            else
                _isAutomating = false;
        }

        private enum MoveCommand
        {
            Forward = 0,
            Backward = 1,
            ToEnd = 2,
            ToStart = 3,
            ToggleAuto = 4
        }

        private static readonly Dictionary<KeyCode, MoveCommand> _keyMapping = new()
        {
            { KeyCode.RightArrow, MoveCommand.Forward },
            { KeyCode.LeftArrow, MoveCommand.Backward },
            { KeyCode.Space, MoveCommand.ToggleAuto },
            { KeyCode.UpArrow, MoveCommand.ToEnd },
            { KeyCode.DownArrow, MoveCommand.ToStart },
        };
        
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

        private void HandleKeyboardShortcuts()
        {
            foreach (var shortcut in _keyMapping)
                if (Input.GetKeyDown(shortcut.Key))
                {
                    HandleMoveControls((int)shortcut.Value);
                    return;
                }
        }

        /// <summary>
        /// Check if all the moves are done.
        /// </summary>
        /// <returns></returns>
        private bool CanMoveForward() => _currentIndex < _moveQueue.Count;

        /// <summary>
        /// Check if currently on the first move.
        /// </summary>
        /// <returns></returns>
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
        
        /// <summary>
        /// When new solution arrives, enqueue moves and start solving
        /// </summary>
        private void HandleNewMoveList()
        {
            // Prevent setting move list for shuffles or when editing
            if (_newSolution)
            {
                Manager.Instance.UpdateMoveList();
                _newSolution = false;
            }
        }

        public void SetIsSolving(bool b) => _isSolving = b;

        public bool IsSolving() => _isSolving;

        public void TurnOffAutomation() => _isAutomating = false;
    }
}
