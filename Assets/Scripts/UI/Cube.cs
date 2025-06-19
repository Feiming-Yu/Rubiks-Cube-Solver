using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{
    public Transform piecePrefab;

    private List<Transform> pieceTransformList = new();

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
            if (k == -1 || k == 0)
                Destroy(piece.GetChild(0).gameObject);
            if (k == 1 || k == 0)
                Destroy(piece.GetChild(1).gameObject);
            if (j == -1 || j == 0)
                Destroy(piece.GetChild(2).gameObject);
            if (j == 1 || j == 0)
                Destroy(piece.GetChild(3).gameObject);
            if (i == -1 || i == 0)
                Destroy(piece.GetChild(4).gameObject);
            if (i == 1 || i == 0)
                Destroy(piece.GetChild(5).gameObject);

            pieceTransformList.Add(piece);
        }
    }
}
