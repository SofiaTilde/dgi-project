//using JetBrains.Annotations;
//using UnityEngine;
using System;
using System.Collections.Generic;

//using OpenCover.Framework.Model;

namespace Classes
{
    public class Start
    {
        public static void Main(string[] args)
        {
            Program program = new Program();
        }
    }

    public class Program
    {
        public SortedDictionary<string, Internode> internodes;
        public SortedDictionary<string, Petiole> petioles;
        public SortedDictionary<string, Leaf> leaves;

        public Program()
        {
            internodes = new SortedDictionary<string, Internode>();
            petioles = new SortedDictionary<string, Petiole>();
            leaves = new SortedDictionary<string, Leaf>();
            internodes.Add(
                "a",
                new Internode(1, 4, "a", 3, ref internodes, ref petioles, ref leaves)
            );

            Console.WriteLine("Internodes:");
            foreach (KeyValuePair<string, Internode> kvp in internodes)
            {
                Console.WriteLine(kvp.Key, " ", kvp.Value.ID);
            }
            Console.WriteLine("\nPetioles:");
            foreach (KeyValuePair<string, Petiole> kvp in petioles)
            {
                Console.WriteLine(kvp.Key, " ", kvp.Value.ID);
            }
            Console.WriteLine("\nLeaves:");
            foreach (KeyValuePair<string, Leaf> kvp in leaves)
            {
                Console.WriteLine(kvp.Key, " ", kvp.Value.ID);
            }
        }
    }

    public class Internode
    {
        public (int, int) Size; //thickness, length

        //public Color MainColor;
        public string ID;
        public int Depth;
        public string PetioleId;
        public string InternodeId;

        public Internode(
            int age,
            int lightPower,
            string id,
            int depth,
            ref SortedDictionary<string, Internode> internodes,
            ref SortedDictionary<string, Petiole> petioles,
            ref SortedDictionary<string, Leaf> leaves
        )
        {
            ID = id;
            Depth = depth;
            PetioleId = ID + "p";
            petioles.Add(
                PetioleId,
                new Petiole(
                    age,
                    lightPower,
                    PetioleId,
                    depth - 1,
                    ref internodes,
                    ref petioles,
                    ref leaves
                )
            );
            if (depth != 0)
            {
                InternodeId = ((Char)(Convert.ToUInt16(ID[0]) + 1)).ToString();
                internodes.Add(
                    InternodeId,
                    new Internode(
                        age,
                        lightPower,
                        InternodeId,
                        depth - 1,
                        ref internodes,
                        ref petioles,
                        ref leaves
                    )
                );
            }
            else
            {
                InternodeId = "";
            }
        }
    }

    public class Petiole
    {
        public int Length;
        public (int, int) Thickness; //start and end thickness
        public int Angle; //-45 to 45
        public int Rotation; //-180 to 180

        //public Color MainColor;
        public string ID;
        public int Depth;
        public string LeafId;

        public Petiole(
            int age,
            int lightPower,
            string id,
            int depth,
            ref SortedDictionary<string, Internode> internodes,
            ref SortedDictionary<string, Petiole> petioles,
            ref SortedDictionary<string, Leaf> leaves
        )
        {
            ID = id;
            Depth = depth;

            if (depth == 0)
            {
                Angle = 0;
            }
            else
            {
                //randomize angle
            }
            LeafId = ID + "l";
            leaves.Add(
                LeafId,
                new Leaf(
                    age,
                    lightPower,
                    LeafId,
                    depth - 1,
                    ref internodes,
                    ref petioles,
                    ref leaves
                )
            );
        }
    }

    public class Leaf
    {
        public (int, int) Holes;
        public (int, int) Size; //width, height

        //public Color MainColor;
        //public Color VeinColor;
        public string ID;
        public int Depth;

        public Leaf(
            int age,
            int lightPower,
            string id,
            int depth,
            ref SortedDictionary<string, Internode> internodes,
            ref SortedDictionary<string, Petiole> petioles,
            ref SortedDictionary<string, Leaf> leaves
        )
        {
            ID = id;
            Depth = depth;
            if (depth == 0)
            {
                //lighter color
            }
        }
    }
}
