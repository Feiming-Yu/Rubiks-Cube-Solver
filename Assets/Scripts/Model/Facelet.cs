using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static UI.Square;

namespace Model
{
    public class Facelet
    {
        /*
           0 1 2
           3 F 4
           5 6 7
         */

        public Dictionary<int, List<int>> Faces = new();

        // D U B F L R
        // Order of the faces have been concatenated
        //
        // Allows storage of squares in one list
        public List<int> Concat()
        {
            return Faces.Values.SelectMany(x => x).ToList();
        }

        public void Add(int face, List<int> squares)
        {
            Faces.Add(face, squares);
        }

        public void Log()
        {
            string message = "";
            foreach (var face in Faces)
            {
                message += $"{ColourToString(face.Key)} FACE : {string.Join(" ", face.Value)}\n";
            }
            
            Debug.Log(message);
        }
    }
}
