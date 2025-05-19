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
            Program program = new Program(int.Parse(args[0]), int.Parse(args[1]));
        }

        public static int Rand(int start, int end)
        {
            Random rand = new Random();
            return rand.Next(start, end);
        }
    }

    public class Program
    {
        public SortedDictionary<string, Internode> internodes;
        public SortedDictionary<string, Petiole> petioles;
        public SortedDictionary<string, Leaf> leaves;
        public int pot; //1=small, 2=medium, 3=big
        public float plantRoughLength;
        public int depth;
        public int Age; //1 to 5
        public int LightPower; //1 to 5

        public Program(int age, int lightPower)
        {
            Age = age;
            LightPower = lightPower;
            Random rand = new Random();
            depth = age * 2 + Start.Rand(0, 2);
            internodes = new SortedDictionary<string, Internode>();
            petioles = new SortedDictionary<string, Petiole>();
            leaves = new SortedDictionary<string, Leaf>();
            internodes.Add(
                "a",
                new Internode(age, lightPower, "a", depth, ref internodes, ref petioles, ref leaves)
            );

            plantRoughLength = 0;
            Console.WriteLine("Internodes:");
            foreach (KeyValuePair<string, Internode> kvp in internodes)
            {
                Console.Write(kvp.Key + " " + kvp.Value.ID);
                Console.Write(" Thic:" + kvp.Value.Thickness.ToString("0.###"));
                Console.Write(" Len:" + kvp.Value.Length.ToString("0.###"));
                Console.Write(" Ang:" + kvp.Value.Angle);
                Console.Write(" Rot:" + kvp.Value.Rotation);
                Console.WriteLine();
                plantRoughLength += kvp.Value.Length;
            }
            Console.WriteLine("\nPetioles:");
            foreach (KeyValuePair<string, Petiole> kvp in petioles)
            {
                Console.Write(kvp.Key + " " + kvp.Value.ID);
                Console.Write(" ThicStart:" + kvp.Value.ThicknessStart.ToString("0.###"));
                Console.Write(" ThicEnd:" + kvp.Value.ThicknessEnd.ToString("0.###"));
                Console.Write(" Width:" + kvp.Value.WidthEnd.ToString("0.###"));
                Console.Write(" Len:" + kvp.Value.Length.ToString("0.###"));
                Console.Write(" Ang:" + kvp.Value.Angle);
                Console.Write(" Rot:" + kvp.Value.Rotation);
                Console.WriteLine();
            }
            Console.WriteLine("\nLeaves:");
            foreach (KeyValuePair<string, Leaf> kvp in leaves)
            {
                Console.Write(kvp.Key + " " + kvp.Value.ID);
                Console.Write(" ModelId:" + kvp.Value.LeafModelId);
                Console.WriteLine();
            }
            if (depth < 4)
            {
                pot = 1;
            }
            else if (depth < 8)
            {
                pot = 2;
            }
            else
            {
                pot = 3;
            }
            Console.WriteLine("\nPot: " + pot);
        }
    }

    public class Internode
    {
        public float Thickness; //thickness, length
        public float Length; //thickness, length

        //public Color MainColor;
        public string ID;
        public int Depth;
        public int Angle; //0 to 45
        public int Rotation; //-180 to 180
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
            Length = 0.15f - (0.02f * lightPower);
            Thickness = 0.005f + (0.005f * age);
            Angle = Start.Rand(0, 45);
            Rotation = Start.Rand(-180, 180);
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
            if (depth > 1)
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
        public float Length;
        public float ThicknessStart;
        public float ThicknessEnd;
        public float WidthEnd;
        public int Angle; //15 to 60
        public int Rotation; //20 to 90 or 200 to 270

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
            ThicknessStart = 0.005f + (0.005f * age);
            ThicknessEnd = 0.003f + 0.001f * age;
            Length = 0.1625f + (0.0875f * age);
            Angle = Start.Rand(15, 60);
            Rotation = Start.Rand(20, 90);
            if (depth % 2 == 0)
            {
                Rotation += 180;
            }
            if (depth == 1)
            {
                Angle = 0;
            }
            LeafId = ID + "l";
            leaves.Add(
                LeafId,
                new Leaf(
                    age,
                    lightPower,
                    LeafId,
                    depth - 1,
                    Angle,
                    Rotation,
                    ref internodes,
                    ref petioles,
                    ref leaves
                )
            );
            int LeafModelId = leaves[LeafId].LeafModelId;
            WidthEnd = ThicknessEnd - 0.0028f + 0.0014f * LeafModelId;
        }
    }

    public class Leaf
    {
        public (int, int) Holes;
        public (float, float) Size; //width, height
        public int LeafModelId; //1-10

        //public Color MainColor;
        //public Color VeinColor;
        public string ID;
        public int Depth;
        public int Angle;
        public int Rotation;

        public Leaf(
            int age,
            int lightPower,
            string id,
            int depth,
            int angle,
            int rotation,
            ref SortedDictionary<string, Internode> internodes,
            ref SortedDictionary<string, Petiole> petioles,
            ref SortedDictionary<string, Leaf> leaves
        )
        {
            ID = id;
            Depth = depth;
            Angle = angle;
            Rotation = rotation;
            LeafModelId = (age * 2) - (6 - lightPower) - (depth / 2) + Start.Rand(-1, 2);
            if (LeafModelId < 1)
            {
                //Console.WriteLine("id:" + LeafModelId);
                LeafModelId = 1;
            }
            if (LeafModelId > 10)
            {
                //Console.WriteLine("id:" + LeafModelId);
                LeafModelId = 10;
            }
        }
    }
}
