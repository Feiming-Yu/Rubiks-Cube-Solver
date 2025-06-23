using System.Collections.Generic;
using Model;
using UnityEngine;

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

        private Cubie _cubie;

        private Facelet _facelet;

        private List<Transform> _pieceTransformList = new();

        private bool _initList;

        private void Update()
        {
            if (_initList)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    _pieceTransformList.Add(transform.GetChild(i));
                }
                
                _initList = false;

                GenerateFacelet();
                _cubie = Converter.FaceletToCubie(_facelet);
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                GenerateFacelet();
                _facelet.Log();
                _cubie = Converter.FaceletToCubie(_facelet);
                _cubie.Log();
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                _cubie.Move("U");
                _facelet = Converter.CubieToFacelet(_cubie);
                SetColours(_facelet);
                _facelet.Log();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                _cubie.Move("D");
                _facelet = Converter.CubieToFacelet(_cubie);
                SetColours(_facelet);
                _facelet.Log();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                _cubie.Move("F");
                _facelet = Converter.CubieToFacelet(_cubie);
                SetColours(_facelet);
                _facelet.Log();
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                _cubie.Move("B");
                _facelet = Converter.CubieToFacelet(_cubie);
                SetColours(_facelet);
                _facelet.Log();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                _cubie.Move("R");
                _facelet = Converter.CubieToFacelet(_cubie);
                SetColours(_facelet);
                _facelet.Log();
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                _cubie.Move("L");
                _facelet = Converter.CubieToFacelet(_cubie);
                SetColours(_facelet);
                _facelet.Log();
            }
        }

        private void Start()
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


        private readonly int[][] _colourPiecePositions = new int[6][]
        {
            new int [8] { 18, 19, 20, 9, 11, 0, 1, 2 },
            new int [8] { 6, 7, 8, 15, 17, 24, 25, 26 },
            new int [8] { 24, 25, 26, 21, 23, 18, 19, 20 },
            new int [8] { 8, 7, 6, 5, 3, 2, 1, 0 },
            new int [8] { 26, 17, 8, 23, 5, 20, 11, 2 },
            new int [8] { 6, 15, 24, 3, 21, 0, 9, 18 },
        };

        private readonly int[][] _colourSquarePositions = new int[6][]
        {
            new int [8] { 1, 0, 1, 1, 1, 1, 0, 1 },
            new int [8] { 1, 0, 1, 1, 1, 1, 0, 1 },
            new int [8] { 2, 1, 2, 1, 1, 2, 1, 2 },
            new int [8] { 2, 1, 2, 1, 1, 2, 1, 2 },
            new int [8] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new int [8] { 0, 0, 0, 0, 0, 0, 0, 0 }
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
                    AddToFacelet(_facelet, i, _colourPiecePositions[i][j], _colourSquarePositions[i][j]);
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
            for (int i = 0; i <= 5; i++)
            for (int j = 0; j < 8; j++)
            {
                SetSquareColour(f.Faces[i][j], _colourPiecePositions[i][j], _colourSquarePositions[i][j]);
            }
        }

        private void SetSquareColour(int colour, int pieceIndex, int squareIndex)
        {
            var square = _pieceTransformList[pieceIndex].GetChild(squareIndex).GetComponent<Square>();
            square.colour = colour;
            square.UpdateColour();
        }
    }
}
