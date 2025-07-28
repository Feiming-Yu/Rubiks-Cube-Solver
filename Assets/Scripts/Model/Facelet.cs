using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static UI.Square;
using System;

namespace Model
{
    [Serializable]
    public class Facelet
    {
        /*
           0 1 2
           3 F 4
           5 6 7
         */

        [Serializable]
        public class SerializableFace
        {
            public List<int> squares;

            public SerializableFace(List<int> squares)
            {
                this.squares = squares;
            }
        }

        public Facelet(Facelet f)
        {
            Faces = f.Faces;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Facelet"/> class with no configuration.
        /// This constructor is intentionally left blank and should only be used when setup is handled manually.
        /// </summary>
        public Facelet()
        {
            // Intentionally left blank
        }
        
        public Dictionary<int, List<int>> Faces = new();

        // D U B F L R
        // Order of the faces have been concatenated
        //
        // Allows storage of squares in one list
        public List<int> Concat()
        {
            return Faces.Values.SelectMany(x => x).ToList();
        }

        public List<SerializableFace> ToNestedList()
        {
            return Faces.Select(face => new SerializableFace(face.Value)).ToList();
        }

        public void Add(int face, List<int> squares) => Faces.Add(face, squares);

        public void Log() => Debug.Log(ToString());

        public override string ToString()
        {
            string message = "";
            foreach (var face in Faces)
                message += $"{ColourToString(face.Key)} FACE : {string.Join(" ", face.Value)}\n";

            return message;
        }
    }
}
