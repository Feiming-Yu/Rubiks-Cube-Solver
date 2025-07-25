using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Model;
using UI;
using UnityEngine;
using static UI.Square;
using static Model.Converter;
using static UI.Square.Colour;
using static Model.Cubie;
using System.Linq;
using static Manager;
using static Engine.Solver.ExitCode;
using System.Drawing;

namespace Engine
{
    public class Solver
    {
        public enum ExitCode
        {
            SUCCESS = 0, FAIL = 1, DEFAULT = -1, EXCEPTION = 2
        }

        private interface IStageSolver
        {
            public void Solve();
        }

        private Cubie _cube;

        private CancellationTokenSource _cancellationTokenSource;

        private CancellationToken _token;

        public List<string> Moves { get; private set; }

        private readonly bool _mainThread;

        private WhiteCross _whiteCross;
        private WhiteCorners _whiteCorners;
        private MiddleLayer _middleLayer;
        private YellowCross _yellowCross;
        private YellowEdges _yellowEdges;
        private YellowCorners _yellowCorners;
        private YellowCornerOris _yellowCornerOris;

        private ExitCode _exitCode;
        private readonly int _testNumber;

        private Facelet _initialState;

        public Solver(bool mainThread = true, int testNumber = -1)
        {
            _mainThread = mainThread;
            _testNumber = testNumber;
        }

        public async Task SolveAsync(Cubie cube, int stage)
        {
            _cube = _mainThread ? new Cubie(cube) : cube;
            _initialState = CubieToFacelet(cube);
            await StartThreadedSolveAsync(stage);
        }

        private async Task StartThreadedSolveAsync(int stage = 0)
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource = cancellationTokenSource;
            _token = cancellationTokenSource.Token;

            InitializeSolvers();

            _exitCode = DEFAULT; // Reset exit code

            // run the solving process asynchronously on a background thread
            var solveTask = Task.Run(() => SolveCube(stage), _token);

            TimeSpan timeoutDuration = TimeSpan.FromMilliseconds(500);
            var timeoutTask = Task.Delay(timeoutDuration, _token);

            // wait for either solve to complete or timeout to occur
            var completedTask = await Task.WhenAny(solveTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // cancel the solving operation if timeout occurs
                _cancellationTokenSource.Cancel();

                if (_exitCode == DEFAULT)
                    TimeOutThread();
            }

            // await the solve task to ensure full completion or cancellation handling
            await solveTask;
        }

        private void InitializeSolvers()
        {
            _whiteCross = new WhiteCross(this, _token, _cube);
            _whiteCorners = new WhiteCorners(this, _token, _cube);
            _middleLayer = new MiddleLayer(this, _token, _cube);
            _yellowCross = new YellowCross(this, _token, _cube);
            _yellowEdges = new YellowEdges(this, _token, _cube);
            _yellowCorners = new YellowCorners(this, _token, _cube);
            _yellowCornerOris = new YellowCornerOris(this, _token, _cube);
        }

        private void SolveCube(int stage)
        {
            Moves = new List<string>();

            try
            {
                switch (stage)
                {
                    case 0:
                        _whiteCross.Solve();
                        _whiteCorners.Solve();
                        _middleLayer.Solve();
                        _yellowCross.Solve();
                        _yellowEdges.Solve();
                        _yellowCorners.Solve();
                        _yellowCornerOris.Solve();
                        if (!CheckSolved())
                            throw new Exception("Cube not solved");

                        break;
                    case 1:
                        _whiteCross.Solve();
                        break;
                    case 2:
                        _whiteCorners.Solve();
                        break;
                    case 3:
                        _middleLayer.Solve();
                        break;
                    case 4:
                        _yellowCross.Solve();
                        break;
                    case 5:
                        _yellowEdges.Solve();
                        break;
                    case 6:
                        _yellowCorners.Solve();
                        break;
                    case 7:
                        _yellowCornerOris.Solve();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(stage), $"Invalid stage: {stage}");
                }


                _token.ThrowIfCancellationRequested();

                if (_mainThread)
                {
                    Cube.Instance.EnqueueSolution(new Queue<string>(Moves));
                    Debug.LogWarning("Solver status: Successful");
                }

                _exitCode = SUCCESS;
            }
            catch (Exception e) 
            {
                Debug.LogError(e);
                Cube.Instance.EnqueueSolution(new Queue<string>(Moves));
                _exitCode = EXCEPTION;
                SaveLog();
            }
        }

        // Note: called outside of Unity main thread
        private void TimeOutThread()
        {
            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                if (_exitCode == DEFAULT)
                    _exitCode = FAIL;

                if (_mainThread)
                {
                    Debug.LogWarning("[TIME OUT] Solver status: Unsuccessful");
                    // queue the correct moves prior to the error
                    Cube.Instance.EnqueueSolution(new Queue<string>(Moves));
                }
                else
                    SaveLog();
            }
        }

        private void SaveLog()
        {
            var cubeFile = new CubeFile(_initialState, Moves, _testNumber, _exitCode);
            LogQueue.Enqueue(cubeFile);
        }

        private class WhiteCross : IStageSolver
        {
            private readonly CancellationToken _token;
            private readonly Cubie _cube;
            private readonly Solver _solver;

            public WhiteCross(Solver solver, CancellationToken token, Cubie cube)
            {
                _token = token;
                _cube = cube;
                _solver = solver;
            }

            public void Solve()
            {
                if (CheckIfWhiteCross())
                    return;

                SolveDaisy();

                SolveWhiteCross();
            }

            #region DAISY

            private void SolveDaisy()
            {
                // tracks which pieces are solved
                List<List<int>> daisyWhiteEdges = new();

                while (daisyWhiteEdges.Count < 4)
                {
                    if (_token.IsCancellationRequested)
                        _token.ThrowIfCancellationRequested();

                    foreach (var edge in _cube.Edges)
                    {
                        if (!IsWhitePiece(edge.Value) || daisyWhiteEdges.Contains(edge.Value.colours))
                            continue;

                        if (IsEdgeInTopLayer(edge) && !IsUpright(edge.Value))
                            FlipEdge(edge);
                        else if (IsEdgeInMiddleLayer(edge))
                            MiddleToTop(edge);
                        else if (IsEdgeInBottomLayer(edge))
                            BottomToTop(edge);

                        daisyWhiteEdges.Add(edge.Value.colours);

                        break;
                    }
                }
            }

            #region HELPERS

            private bool IsEdgeInTopLayer(KeyValuePair<int, Piece> piece) => piece.Key < 4;

            private bool IsEdgeInMiddleLayer(KeyValuePair<int, Piece> piece) => piece.Key > 7;

            private bool IsEdgeInBottomLayer(KeyValuePair<int, Piece> piece) => piece.Key is > 3 and < 8;

            private bool IsWhitePiece(Piece piece) => piece.colours.Contains(WHITE);

            private bool IsUpright(Piece piece) => piece.orientation == 0;

            private bool IsUprightWhitePiece(Piece piece) => IsWhitePiece(piece) && IsUpright(piece);

            #endregion

            private void FlipEdge(KeyValuePair<int, Piece> edge)
            {
                int currentFace = SideFaces[edge.Key];
                int leftFace = SideFaces[(edge.Key + 1) % 4];

                // ensures all side rotations are clockwise
                string currentPrime = currentFace != GREEN ? "'" : "";
                string leftPrime = leftFace != GREEN ? "'" : "";


                _solver.PerformMove(ColourToFace(currentFace) + currentPrime);

                if (IsUprightWhitePiece(_cube.Edges[(edge.Key + 1) % 4]))
                    _solver.PerformMove("U");

                _solver.PerformMove(ColourToFace(leftFace) + leftPrime);
            }

            private void MiddleToTop(KeyValuePair<int, Piece> edge)
            {
                int whiteSquareIncrement = edge.Key % 2 == 1 ? 1 - edge.Value.orientation : edge.Value.orientation;

                int faceIndex = (edge.Key - 8 + whiteSquareIncrement) % 4;
                int face = SideFaces[faceIndex];

                FindEmptyUSlot(faceIndex);

                string prime = !((edge.Key % 2 != edge.Value.orientation) ^ (face != GREEN)) ? "'" : "";
                _solver.PerformMove(ColourToFace(face) + prime);
            }

            private void BottomToTop(KeyValuePair<int, Piece> edge)
            {
                int faceIndex = (edge.Key - 4) % 4;
                int face = SideFaces[faceIndex];

                FindEmptyUSlot(faceIndex);

                if (edge.Value.orientation == 0)
                    _solver.PerformMove(ColourToFace(face) + "2");
                else
                {
                    string facePrime = face == GREEN ? "'" : "";
                    _solver.PerformMove(ColourToFace(face) + facePrime);

                    if (IsUprightWhitePiece(_cube.Edges[(faceIndex + 1) % 4]))
                        _solver.PerformMove("U");

                    int leftFace = SideFaces[(faceIndex + 1) % 4];
                    string leftFacePrime = leftFace == GREEN ? "" : "'";

                    _solver.PerformMove(ColourToFace(leftFace) + leftFacePrime);
                }
            }

            private void FindEmptyUSlot(int faceIndex)
            {
                int occupiedPieceIndex = faceIndex;
                int UTurns = 0;

                while (IsUprightWhitePiece(_cube.Edges[occupiedPieceIndex]))
                {
                    UTurns++;
                    if (occupiedPieceIndex == 0)
                        occupiedPieceIndex = 3;
                    else
                        occupiedPieceIndex--;
                }

                _solver.DoUTurns(UTurns);
            }

            #endregion

            #region WHITE CROSS

            private bool CheckIfWhiteCross()
            {
                foreach (var edge in _cube.Edges)
                {
                    if (IsWhitePiece(edge.Value) && (!IsEdgeInBottomLayer(edge) || !IsUpright(edge.Value)))
                        return false;
                }
                return true;
            }

            private void SolveWhiteCross()
            {
                // tracks which pieces are solved
                List<List<int>> crossWhiteEdges = new();

                while (crossWhiteEdges.Count < 4)
                {
                    if (_token.IsCancellationRequested)
                        _token.ThrowIfCancellationRequested();

                    foreach (var edge in _cube.Edges)
                    {
                        if (!IsWhitePiece(edge.Value) || crossWhiteEdges.Contains(edge.Value.colours))
                            continue;

                        TopToBottom(edge);
                        crossWhiteEdges.Add(edge.Value.colours);
                        break;
                    }
                }
            }

            private void TopToBottom(KeyValuePair<int, Piece> edge)
            {
                int currentFaceIndex = edge.Key;

                int targetFaceIndex = SideFaces.IndexOf(edge.Value.colours[1]);

                _solver.DoUTurns((targetFaceIndex - currentFaceIndex + 4) % 4);

                _solver.PerformMove(ColourToFace(edge.Value.colours[1]) + "2");
            }

            #endregion
        }

        private class WhiteCorners : IStageSolver
        {
            private readonly CancellationToken _token;
            private readonly Cubie _cube;
            private readonly Solver _solver;

            public WhiteCorners(Solver solver, CancellationToken token, Cubie cube)
            {
                _token = token;
                _cube = cube;
                _solver = solver;
            }

            public void Solve()
            {
                // tracks which pieces are solved
                List<List<int>> solvedCorners = new();

                List<int> currentWhiteCorner = null;

                while (solvedCorners.Count < 4)
                {
                    if (_token.IsCancellationRequested)
                        _token.ThrowIfCancellationRequested();

                    foreach (var corner in _cube.Corners)
                    {
                        var colours = corner.Value.colours;

                        if (!IsWhite(colours))
                            continue;

                        if (!IsUnsolved(colours))
                            continue;

                        if (IsAlreadySolved(corner))
                        {
                            MarkAsSolved(colours);
                            continue;
                        }

                        if (!IsCurrentWhiteCorner(colours))
                            continue;

                        bool isAtTop = IsTopLayer(corner.Key);

                        if (isAtTop && !AlignWithHome(corner.Key, colours))
                            break;

                        SolveCorner(corner, isAtTop);

                        if (isAtTop)
                            MarkAsSolved(colours);

                        break;
                    }

                    #region HELPERS

                    bool IsAlreadySolved(KeyValuePair<int, Piece> corner)
                    {
                        return FindHomeIndex(corner.Value.colours) == corner.Key && corner.Value.orientation == 0;
                    }

                    bool IsWhite(List<int> colours) => colours.Contains(WHITE);

                    bool IsUnsolved(List<int> colours) => !solvedCorners.Contains(colours);

                    bool IsCurrentWhiteCorner(List<int> colours)
                    {
                        currentWhiteCorner ??= colours;
                        return colours.SequenceEqual(currentWhiteCorner);
                    }

                    bool IsTopLayer(int key) => key < 4;

                    bool AlignWithHome(int key, List<int> colours)
                    {
                        int uTurns = CalculateUTurns();
                        _solver.DoUTurns(uTurns);
                        return uTurns == 0;

                        int CalculateUTurns() => (FindHomeIndex(colours) - key + 4) % 4;
                    }

                    void SolveCorner(KeyValuePair<int, Piece> corner, bool isAtTop)
                    {
                        if (corner.Value.orientation == 0 && isAtTop)
                            SolveYellowToWhite(corner);
                        else
                            SolveOrientatedOrMismatched(corner);
                    }

                    void MarkAsSolved(List<int> colours)
                    {
                        solvedCorners.Add(colours);
                        currentWhiteCorner = null;
                    }

                    #endregion
                }
            }
            
            private void SolveYellowToWhite(KeyValuePair<int, Piece> corner)
            {
                int face = SideFaces[corner.Key];
                _solver.PerformMove(ColourToFace(face) + (face == GREEN ? "'" : ""));

                int rightFace = SideFaces[(corner.Key + 3) % 4];
                _solver.PerformMove(ColourToFace(rightFace) + (rightFace == GREEN ? "'" : ""));

                _solver.PerformMove("U2");

                _solver.PerformMove(ColourToFace(rightFace) + (rightFace == GREEN ? "" : "'"));

                _solver.PerformMove(ColourToFace(face) + (face == GREEN ? "" : "'"));
            }

            private void SolveOrientatedOrMismatched(KeyValuePair<int, Piece> corner)
            {
                bool isAtTop = corner.Key < 4;
                int orientation = isAtTop ? corner.Value.orientation : 1;
                int offset = isAtTop ? 0 : -4;
                int face = SideFaces[(corner.Key + (orientation - 1) + offset + 4) % 4];
                bool prime = !((face == GREEN) ^ (orientation == 1));

                string move = ColourToFace(face) + (prime ? "'" : "");
                _solver.PerformMove(move);

                _solver.PerformMove("U" + (orientation == 2 ? "'" : ""));

                string reverseMove = ColourToFace(face) + (prime ? "" : "'");
                _solver.PerformMove(reverseMove);
            }
        }

        private class MiddleLayer : IStageSolver
        {
            private readonly CancellationToken _token;
            private readonly Cubie _cube;
            private readonly Solver _solver;

            public MiddleLayer(Solver solver, CancellationToken token, Cubie cube)
            {
                _token = token;
                _cube = cube;
                _solver = solver;
            }

            public void Solve()
            {
                List<List<int>> solvedMiddleEdges = new();
                List<int> currentMiddleEdge = null;

                while (solvedMiddleEdges.Count < 4)
                {
                    if (_token.IsCancellationRequested)
                        _token.ThrowIfCancellationRequested();

                    foreach (var edge in _cube.Edges)
                    {
                        if (_token.IsCancellationRequested)
                            _token.ThrowIfCancellationRequested();
                        var colours = edge.Value.colours;
                        int edgeIndex = edge.Key;
                        int orientation = edge.Value.orientation;
                        bool isAtTop = edgeIndex < 4;

                        // filter out non-eligible edges
                        if (IsAlreadySolved(colours)) continue;
                        if (ContainsWhiteOrYellow(colours)) continue;
                        if (!IsCurrentTarget(colours)) continue;

                        // check if edge is solved in correct position
                        if (IsCorrectlyPlaced(edgeIndex, orientation, colours))
                            break;

                        // attempt insertion if edge is in top layer
                        if (isAtTop & SwapTopAndSideEdges(edge))
                            MarkAsSolved(colours);

                        break;
                    }

                    continue;

                    #region HELPERS

                    bool IsAlreadySolved(IReadOnlyCollection<int> colours) => solvedMiddleEdges.Any(edge => edge.SequenceEqual(colours));

                    bool ContainsWhiteOrYellow(ICollection<int> colours) => colours.Contains(WHITE) || colours.Contains(YELLOW);

                    bool IsCurrentTarget(List<int> colours)
                    {
                        currentMiddleEdge ??= colours;
                        return colours.SequenceEqual(currentMiddleEdge);
                    }

                    bool IsCorrectlyPlaced(int edgeIndex, int orientation, List<int> colours)
                    {
                        if (orientation == FindHomeOrientation(colours) &&
                            edgeIndex == FindHomeIndex(colours))
                        {
                            MarkAsSolved(colours);
                            return true;
                        }

                        return false;
                    }

                    void MarkAsSolved(List<int> colours)
                    {
                        solvedMiddleEdges.Add(colours);
                        currentMiddleEdge = null;
                    }

                    #endregion
                }
            }

            private bool SwapTopAndSideEdges(KeyValuePair<int, Piece> edge)
            {
                if (_token.IsCancellationRequested)
                    _token.ThrowIfCancellationRequested();
                
                var colours = edge.Value.colours;
                int orientation = edge.Value.orientation;

                int edgeIndex = edge.Key;

                bool isAtTop = edgeIndex < 4;

                // get the color on the top face and the adjacent side face
                int mainFace = !isAtTop ? SideFaces[edgeIndex - 8] : colours[orientation];
                int sideColour = !isAtTop ? SideFaces[(edgeIndex - 8 + 1) % 4] : colours[1 - orientation];

                // find the indices of these colors in the SideFaces list
                int mainIndex = SideFaces.IndexOf(mainFace);
                int sideIndex = SideFaces.IndexOf(sideColour);

                // ff the edge is at the top, rotate U layer to align it for insertion
                if (isAtTop)
                {
                    int UTurnsNeeded = (sideIndex - edgeIndex + 4) % 4;
                    _solver.DoUTurns(UTurnsNeeded);
                    // if rotation was needed, return false to retry later
                    if (UTurnsNeeded != 0) return false;
                }

                // decide whether to insert to the right or left based on color positions
                bool insertRight = ((mainIndex - sideIndex + 4) % 4) == 3;
                
                // determine if the rotation on the side face is prime (counter-clockwise)
                bool faceIsPrime = (sideColour == GREEN) ^ insertRight;
                
                // calculate which side face to rotate and whether it is a prime move
                int sideFace = SideFaces[(sideIndex + (insertRight ? 3 : 1)) % 4];
                bool sideFaceIsPrime = (sideFace == GREEN) ^ !insertRight;


                RotateU(insertRight);
                Rotate(sideFace, sideFaceIsPrime);
                RotateU(!insertRight);
                Rotate(sideFace, !sideFaceIsPrime);

                RotateU(!insertRight);
                Rotate(sideColour, faceIsPrime);
                RotateU(insertRight);
                Rotate(sideColour, !faceIsPrime);
                return true;

                void RotateU(bool clockwise) => _solver.PerformMove("U" + (clockwise ? "" : "'"));

                void Rotate(int colour, bool prime) => _solver.PerformMove(ColourToFace(colour) + (prime ? "'" : ""));
            }
        }

        private class YellowCross : IStageSolver
        {
            private readonly CancellationToken _token;
            private readonly Cubie _cube;
            private readonly Solver _solver;

            public YellowCross(Solver solver, CancellationToken token, Cubie cube)
            {
                _token = token;
                _cube = cube;
                _solver = solver;
            }

            private static readonly List<List<int>> YellowCrossStages = new()
            {
                new List<int>() { 1, 3, 4, 6 }, // cross
                new List<int>() { 3, 4 }, // line
                new List<int>() { 4, 6 }, // L
                new List<int>(), // dot
            };

            private static readonly List<string> StageSwitchingMoves = new()
            {
                "F", "R", "U", "R'", "U'", "F'"
            };

            public void Solve()
            {
                for (int k = 0; k < 4; k++) // maximum 4 stages
                {
                    if (_token.IsCancellationRequested)
                        _token.ThrowIfCancellationRequested();
                    
                    if (TrySolveWithRotations())
                        return;
                }

                // should not reach here if cross is solvable
                throw new Exception("Error: solving needed more than 4 stages");
            }

            private bool TrySolveWithRotations()
            {
                for (int rotations = 0; rotations < 4; rotations++)
                {
                    if (_token.IsCancellationRequested)
                        _token.ThrowIfCancellationRequested();

                    var topFace = CubieToFacelet(_cube).Faces[YELLOW];

                    // check if current face matches any yellow cross stage
                    if (TryMatchStage(topFace, rotations, out bool solved))
                    {
                        if (!solved)
                        {
                            if (rotations == 3)
                            {
                                _solver.RemoveLastMoves(3);
                                _solver.Moves.Add("U'");
                            }
                            // apply algorithm to advance to next stage
                            _solver.PerformMoves(StageSwitchingMoves);
                            break;
                        }

                        // already solved
                        return true;
                    }

                    // rotate top face and retry
                    _solver.PerformMove("U"); 
                }

                return false;
            }

            private bool TryMatchStage(IReadOnlyList<int> topFace, int rotationCount, out bool isAlreadySolved)
            {
                isAlreadySolved = false;

                for (int stageIndex = 0; stageIndex < 4; stageIndex++)
                {
                    if (_token.IsCancellationRequested)
                        _token.ThrowIfCancellationRequested();

                    // fallback: allow match only on last rotation attempt
                    if (stageIndex == 3)
                    {
                        return rotationCount == 3;
                    }

                    // check if all squares in this stage are yellow
                    if (YellowCrossStages[stageIndex].All(square => topFace[square] == YELLOW))
                    {
                        // if stage 0 matches, yellow cross is already solved
                        if (stageIndex == 0)
                            isAlreadySolved = true;

                        return true;
                    }
                }

                return false;
            }
        }

        private class YellowEdges : IStageSolver
        {
            private readonly CancellationToken _token;
            private readonly Cubie _cube;
            private readonly Solver _solver;

            public YellowEdges(Solver solver, CancellationToken token, Cubie cube)
            {
                _token = token;
                _cube = cube;
                _solver = solver;
            }

            private readonly List<string> _edgeSwapMoves = new()
            {
                "R", "U", "R'", "U", "R", "U2", "R'"
            };

            public void Solve()
            {
                while (true)
                {
                    if (_token.IsCancellationRequested)
                        _token.ThrowIfCancellationRequested();

                    var solvedEdgeIndices = GetSolvedEdgeIndices();

                    switch (solvedEdgeIndices.Count)
                    {
                        case 0:
                            _solver.PerformMoves(_edgeSwapMoves);
                            break;
                        case 2:
                            HandleTwoSolvedEdges(solvedEdgeIndices);
                            return;
                        default:
                            HandleAllSolvedEdges(solvedEdgeIndices[0]);
                            return;
                    }
                }
            }

            private List<int> GetSolvedEdgeIndices()
            {
                var solvedEdges = new List<int>();

                for (int currentEdge = 0; currentEdge < 4; currentEdge++)
                {
                    // wrap index to next edge
                    int nextEdge = (currentEdge + 1) % 4;

                    int difference = SideFaces.IndexOf(_cube.Edges[nextEdge].colours[1]) -
                                     SideFaces.IndexOf(_cube.Edges[currentEdge].colours[1]);

                    // check if edges are adjacent in expected order
                    if (difference is 1 or -3)
                    {
                        solvedEdges.Add(nextEdge);
                        solvedEdges.Add(currentEdge);
                    }
                }

                return solvedEdges;
            }

            private void HandleTwoSolvedEdges(IReadOnlyList<int> solvedEdgeIndices)
            {
                int first = solvedEdgeIndices[0];
                int second = solvedEdgeIndices[1];

                // if edges are separated oddly, align before swapping
                if (Math.Abs(first - second) % 2 != 0)
                {
                    _solver.DoUTurns((0 - first + 4) % 4);
                    _solver.PerformMoves(_edgeSwapMoves);
                    _solver.DoUTurns(SideFaces.IndexOf(_cube.Edges[0].colours[1]) % 4);
                }
                else
                    _solver.PerformMoves(_edgeSwapMoves);
            }

            private void HandleAllSolvedEdges(int index)
            {
                int faceIndex = SideFaces.IndexOf(_cube.Edges[index].colours[1]);
                int turns = (faceIndex - index + 4) % 4;
                _solver.DoUTurns(turns);
            }
        }

        private class YellowCorners : IStageSolver
        {
            private readonly CancellationToken _token;
            private readonly Cubie _cube;
            private readonly Solver _solver;

            private readonly List<string> _cornerCyclingMoves = new()
            {
                "U", "R", "U'", "L'", "U", "R'", "U'", "L"
            };

            public YellowCorners(Solver solver, CancellationToken token, Cubie cube)
            {
                _token = token;
                _cube = cube;
                _solver = solver;
            }

            public void Solve()
            {
                if (_token.IsCancellationRequested)
                    _token.ThrowIfCancellationRequested();

                var positionedCorners = GetCorrectlyPositionedCorners();

                switch (positionedCorners.Count)
                {
                    case 4:
                        return;
                    case 1:
                    case 2:
                        AlignCube(positionedCorners[0]);
                        PerformCornerCycles(2);
                        RealignCube(positionedCorners[0]);
                        Solve();
                        break;
                    case 0:
                        PerformCornerCycles(2);
                        Solve();
                        break;
                }
            }

            #region HELPERS

            private List<int> GetCorrectlyPositionedCorners()
            {
                var corners = new List<int>();
                for (int i = 0; i < 4; i++)
                {
                    if (FindHomeIndex(_cube.Corners[i].colours) == i)
                        corners.Add(i);
                }

                return corners;
            }

            private void AlignCube(int cornerIndex)
            {
                int turns = (4 - cornerIndex) % 4;
                _solver.DoUTurns(turns);
            }

            private void RealignCube(int cornerIndex)
            {
                int turns = cornerIndex % 4;
                _solver.DoUTurns(turns);
            }

            private void PerformCornerCycles(int times)
            {
                for (int i = 0; i < times; i++)
                    _solver.PerformMoves(_cornerCyclingMoves);
            }

            #endregion
        }

        private class YellowCornerOris : IStageSolver
        {
            private readonly CancellationToken _token;
            private readonly Cubie _cube;
            private readonly Solver _solver;

            private readonly List<string> _cornerOrientationMoves = new()
            {
                "R'", "D'", "R", "D", "R'", "D'", "R", "D"
            };

            public YellowCornerOris(Solver solver, CancellationToken token, Cubie cube)
            {
                _token = token;
                _cube = cube;
                _solver = solver;
            }

            public void Solve()
            {
                List<List<int>> solvedCorners = new();

                for (int i = 0; i < 4; i++)
                {

                    if (_token.IsCancellationRequested)
                        _token.ThrowIfCancellationRequested();

                    if (solvedCorners.Count == 4)
                        break;

                    if (IsSolved(_cube.Corners[i].colours))
                        continue;

                    if (_cube.Corners[i].orientation == 0)
                    {
                        solvedCorners.Add(_cube.Corners[i].colours);
                        continue;
                    }

                    _solver.DoUTurns(4 - i);
                    _solver.PerformMoves(_cornerOrientationMoves);

                    i = -1;
                }

                AlignTopLayer();

                bool IsSolved(List<int> colours) => solvedCorners.Contains(colours);
            }

            private void AlignTopLayer()
            {
                int turns = (SideFaces.IndexOf(_cube.Edges[0].colours[1])) % 4;
                _solver.DoUTurns(turns);
            }


        }

        private bool CheckSolved()
        {
            for (int i = 0; i < 12; i++)
            {
                if (i < 8)
                    if (_cube.Corners[i].orientation != 0 || FindHomeIndex(_cube.Corners[i].colours) != i)
                        return false;

                if (_cube.Edges[i].orientation != 0 || FindHomeIndex(_cube.Edges[i].colours) != i)
                    return false;
            }

            return true;
        }

        private void DoUTurns(int UTurns)
        {
            PerformMoves(UTurns switch
            {
                3 => new List<string> { "U'" },
                2 => new List<string> { "U2" },
                1 => new List<string> { "U" },
                _ => new List<string>(),
            });
        }

        private void PerformMoves(List<string> moves)
        {
            foreach (var move in moves)
                _cube.Move(move);
            Moves.AddRange(moves);
        }

        private void PerformMove(string move)
        {
            _cube.Move(move);
            Moves.Add(move);
        }

        private void RemoveLastMoves(int n)
        {

            Moves.RemoveRange(Moves.Count - n, n);
        }
    }
}
