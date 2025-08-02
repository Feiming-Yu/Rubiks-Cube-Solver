using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using static Model.Converter;
using UnityEngine;
using Engine;
using static UI.Square;
using UnityEngine.UI;
using Tutorial;
using static Model.Cubie;

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
        [SerializeField] private Material normalBody, highlightedBody;

        public float animationSpeed = 10;
        [HideInInspector] public bool isOrientating;

        // core components
        private Transform _rotationPlane;
        private Cubie _cubie;
        private Facelet _facelet;
        private Facelet _startingFacelet;

        // state management
        private bool _initList;    // Flag to trigger initialization of piece transform list
        private bool _newSolution; // Indicates a new solution has been 
        private bool _isSolving;   // Whether the solver is currently solving
        private bool _isScrambling;// Whether the currently shuffling

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

        private readonly List<Facelet> _previousFacelets = new();

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
            if (_currentIndex != _moveQueue.Count || _isAnimating || _isSolving)
                return;
                
            _moveQueue.Clear();

            // Reset the logical cube to solved state
            SetCubie(Identity);
            UpdateFacelet();

            // Update the colors on the graphical model
            SetColours(_facelet);

            _currentIndex = 0;

            _previousFacelets.Clear();
        }

        public bool IsCubeBusy => _currentIndex != _moveQueue.Count || _isAnimating || _isSolving;

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

        private List<int> _highlightedPieceColours = new();

        public void SetHighlightedPiece()
        {
            _highlightedPieceColours = new List<int>(_currentSequence.InterestPiece);
            
            TryHighlightPiece();
        }

        public void RemoveHighlightedPiece()
        {
            _highlightedPieceColours.Clear();
            TryHighlightPiece();
        }

        private void TryHighlightPiece()
        {
            int count = _highlightedPieceColours.Count;

            if (count == 0) count = 3;


            foreach (var piece in count == 3 ? _cubie.Corners : _cubie.Edges)
            {
                bool match = new HashSet<int>(piece.Value.colours).SetEquals(_highlightedPieceColours);
                print(string.Join("", piece.Value.colours) + " " + string.Join("", _highlightedPieceColours));
                _pieceTransformList[CubieIndexToGraphicIndex[piece.Key][count - 2]]
                    .GetComponent<MeshRenderer>().material = match ? highlightedBody : normalBody;
            }

            foreach (var piece in count == 3 ? _cubie.Edges : _cubie.Corners)
            {
                _pieceTransformList[CubieIndexToGraphicIndex[piece.Key][1 - (count - 2)]]
                    .GetComponent<MeshRenderer>().material = normalBody;
            }
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

            _isScrambling = true;
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

            foreach (var key in _moveInputs.Where(Input.GetKeyDown))
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

        #endregion

        #region Solution

        public void Enqueue(string move) => _moveQueue.Add(move);

        public void EnqueueSolution(IEnumerable<Sequence> solution)
        {
            _solution = new List<Sequence>(solution);
            _newSolution = true;
        }

        public List<Sequence> GetSolution() => _solution;

        public List<string> GetCurrentMoveQueue() => _moveQueue;

        public int GetCurrentIndex() => _currentIndex;

        #endregion

        #region Move Animation   

        private Quaternion _destinationOrientation = Quaternion.identity;
        private Quaternion _startOrientation = Quaternion.identity;

        private bool _isAnimating;

        private List<string> _moveQueue = new();
        private List<Sequence> _solution = new();
        private Sequence _currentSequence;
        private Sequence _wholeSequence;
        private int _currentSequenceIndex;
        private int _currentIndex;
        private string _currentMove;
        private bool _isProgressing = true;

        // Indexes of centrepieces
        private readonly int[] _centrePieces = { 10, 16, 22, 4, 14, 12 };

        public void ClearQueue()
        {
            _moveQueue.Clear();
            _solution.Clear();
            _currentSequence = null;
            _currentIndex = 0;
            _currentSequenceIndex = 0;
        }

        public string GetCurrentMove() => _currentMove;

        public Sequence GetCurrentSequence() => _currentSequence;

        private void Move()
        {
            if (_currentIndex >= _moveQueue.Count)
                return;

            _isProgressing = true;
            string move = GetNextMove();
            if (move == "") return;

            BeginMove(move);
        }

        private void Unmove()
        {
            _isProgressing = false;
            string move = GetPreviousMove();
            if (move == "") return;

            move = ReverseMove(move);
            BeginMove(move);
        }

        private string GetNextMove()
        {
            return _moveQueue[_currentIndex];
        }

        private string GetPreviousMove()
        {
            // If no solution exists, manual move queue
            if (_solution.Count == 0)
            {
                return _moveQueue[--_currentIndex];
            }

            // If at the start of the current sequence
            if (_currentIndex == 0 && Manager.Instance.useStages)
            {
                // If this is the first sequence, nothing to go back to
                if (_currentSequenceIndex == 0)
                    return "";

                // To the previous sequence
                _currentSequence = _solution[--_currentSequenceIndex];
                _moveQueue = _currentSequence.Moves;
                _currentIndex = _moveQueue.Count;
                SetHighlightedPiece();

                // Update move list
                Manager.Instance.UpdateMoveList();
            }
            else
            {
                TryHighlightPiece();
                // Update move highlight
                Manager.Instance.SwitchMoveText(false);
            }

            // Return the previous move in the current move queue
            return _moveQueue[--_currentIndex];
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
                (_isScrambling ? 40f : animationSpeed) * 100 * Time.deltaTime);

            // Keep rotating until reached
            if (_rotationPlane.localRotation != _destinationOrientation) return;

            // Undo rotation ...
            _rotationPlane.localRotation = _startOrientation;
            UngroupPieces();

            // ... then do an instant rotation after the cubie rotation
            _cubie.Move(_currentMove);
            UpdateFacelet();
            SetColours(_facelet);

            // Exit if not progressing or not currently solving
            if (!_isProgressing || !IsSolving())
            {
                if (!_isProgressing)
                    TryHighlightPiece();
                else
                    _currentIndex++;
                
                _isAnimating = false;

                // Stop scrambling if all moves are done
                if (_isScrambling && _currentIndex == _moveQueue.Count)
                    _isScrambling = false;

                return;
            }


            PostAnimUpdate();

            // End of animation step
            _isAnimating = false;
            return;

            void PostAnimUpdate()
            {
                // Check if reached the end of current move sequence
                if (_currentIndex == _moveQueue.Count - 1 && Manager.Instance.useStages)
                {
                    if (_isScrambling)
                        _isScrambling = false;

                    // If last sequence, stop automation
                    if ((Manager.Instance.useStages && _currentSequenceIndex == _solution.Count - 1)
                        || (!Manager.Instance.useStages && _currentIndex == _moveQueue.Count - 1))
                    {
                        _isAutomating = false;
                        _isAnimating = false;
                        _currentIndex++;
                        Manager.Instance.SwitchMoveText(); // Show final move as complete
                        return;
                    }

                    // Move to the next sequence
                    _currentSequence = _solution[++_currentSequenceIndex];
                    _moveQueue = _currentSequence.Moves;
                    _currentIndex = 0;
                    SetHighlightedPiece();
                    Manager.Instance.UpdateMoveList(); // Refresh move list
                }
                else
                {
                    TryHighlightPiece();
                    _currentIndex++;
                    // Continue within the current move queue
                    Manager.Instance.SwitchMoveText();
                }
            }
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
            animationSpeed = Mathf.Approximately(slider.value, 6f) ? 30f : slider.value;
        }

        public Stage GetCurrentStage()
        {
            return Stage.Stages[_solution[_currentSequenceIndex].StageIndex];
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

            // Only move if automating or in the editor for shuffling
            // and only if the previous animation has finished
            if ((!_isAutomating && _isSolving) || _isAnimating)
                return;

            if (_currentIndex < _moveQueue.Count || IsSolving())
                Move();
            // Automation finished all the moves
            else if (_currentIndex == _moveQueue.Count && _currentSequenceIndex == _solution.Count - 1)
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

        private static readonly Dictionary<KeyCode, MoveCommand> KeyMapping = new()
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
                    if (isBusy) return;
                    
                    Move();
                    break;

                case MoveCommand.Backward:
                    if (isBusy) return;
                    
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
            foreach (var shortcut in KeyMapping)
                if (Input.GetKeyDown(shortcut.Key))
                {
                    // HandleMoveControls((int)shortcut.Value);
                    
                    print(Instance.GetCurrentStage().ToString());
                    return;
                }
        }

        private void SetToEnd()
        {
            SetCubie(Identity);
            UpdateFacelet();
            SetColours(_facelet);

            if (Manager.Instance.useStages)
            {
                print("Setting to end");
                _currentSequence = _solution[^1];
                _moveQueue = _currentSequence.Moves;
                _currentSequenceIndex = _solution.Count - 1;
            }

            _currentIndex = _moveQueue.Count;

            Manager.Instance.UpdateMoveList();
            SetHighlightedPiece();
        }

        private void SetToStart()
        {
            SetFacelet(_startingFacelet);
            UpdateCubie();
            SetColours(_facelet);

            if (Manager.Instance.useStages)
            {
                _currentSequence = _solution[0];
                _moveQueue = _currentSequence.Moves;
                _currentSequenceIndex = 0;
            }

            _currentIndex = 0;

            Manager.Instance.UpdateMoveList();
            SetHighlightedPiece();
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
                _currentSequence = _solution[0];
                _moveQueue = _currentSequence.Moves;
                _currentIndex = 0;
                _currentSequenceIndex = 0;
                Manager.Instance.UpdateMoveList();
                SetHighlightedPiece();
                CreateWholeSequence();

                if (!Manager.Instance.useStages)
                    SetIndexFromSequence();

                _newSolution = false;
            }
        }

        private void CreateWholeSequence()
        {
            var moves = _solution.SelectMany(s => s.Moves).ToList();

            _wholeSequence = new Sequence(-1, moves, new() { });
        }

        public void SetSequenceFromIndex()
        {
            int cumulativeIndex = 0;

            foreach (var sequence in _solution)
            {
                cumulativeIndex += sequence.Moves.Count;
                
                if (cumulativeIndex <= _currentIndex && cumulativeIndex != _wholeSequence.Moves.Count) 
                    continue;

                _currentSequence = sequence;
                _currentSequenceIndex = _solution.IndexOf(sequence);
                _moveQueue = sequence.Moves;
                _currentIndex -= (cumulativeIndex - sequence.Moves.Count);
                Manager.Instance.SetTutorialButtonActive(true);
                Manager.Instance.UpdateMoveList();
                SetHighlightedPiece();
                return;
            }
        }

        public bool LastSequence => _currentSequenceIndex == _solution.Count - 1;

        public void SetIndexFromSequence()
        {
            _currentIndex += _solution.Take(_currentSequenceIndex).Sum(s => s.Moves.Count);

            _currentSequence = _wholeSequence;
            _currentSequenceIndex = -1;
            _moveQueue = _currentSequence.Moves;
            Manager.Instance.SetTutorialButtonActive(false);
            Manager.Instance.UpdateMoveList();
            SetHighlightedPiece();
        }

        public void SetIsSolving(bool b) => _isSolving = b;

        public bool IsSolving() => _isSolving;

        public void TurnOffAutomation() => _isAutomating = false;
    }
}
