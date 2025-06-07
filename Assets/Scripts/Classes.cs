//using JetBrains.Annotations;
using UnityEngine;
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

        // Function to generate random value
        public static int Rand(int start, int end)
        {
            System.Random rand = new System.Random();
            return rand.Next(start, end);
        }
    }

    public class Program
    {
        public SortedDictionary<string, Internode> internodes;
        public SortedDictionary<string, Petiole> petioles;
        public SortedDictionary<string, Leaf> leaves;
        public int pot; //1=small, 2=medium, 3=big
        public int depth;
        public int Age; //1 to 5
        public int LightPower; //1 to 5
        private GameObject smallPot;
        private GameObject mediumPot;
        private GameObject largePot;

        public Program(int age, int lightPower)
        {
            Age = age;
            LightPower = lightPower;
            System.Random rand = new System.Random();

            // Determine plant depth, this number decreases with 1 for each internode added until reach top internode at depth 1
            depth = age * 2 + Start.Rand(0, 2);

            // Create dictionaries for each plant part
            internodes = new SortedDictionary<string, Internode>();
            petioles = new SortedDictionary<string, Petiole>();
            leaves = new SortedDictionary<string, Leaf>();

            // Start plant generation by adding first internode with name 'a' to internode dictionary
            internodes.Add(
                "a",
                new Internode(age, lightPower, "a", depth, ref internodes, ref petioles, ref leaves)
            );

            // Code for testing purposes
            Console.WriteLine("Internodes:");
            foreach (KeyValuePair<string, Internode> kvp in internodes)
            {
                Console.Write(kvp.Key + " " + kvp.Value.ID);
                Console.Write(" Thic:" + kvp.Value.Thickness.ToString("0.###"));
                Console.Write(" Len:" + kvp.Value.Length.ToString("0.###"));
                Console.Write(" Ang:" + kvp.Value.Angle);
                Console.Write(" Rot:" + kvp.Value.Rotation);
                Console.WriteLine();
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
                //Console.Write(" ModelId:" + kvp.Value.LeafModelId);
                Console.WriteLine();
            }

            // Initialise plant pots
            smallPot = GameObject.Find("small_pot");
            mediumPot = GameObject.Find("medium_pot");
            largePot = GameObject.Find("large_pot");

            // Determine which pot is used for current plant based on total plant depth
            if (depth < 4)
            {
                pot = 1;
                smallPot.GetComponent<Renderer>().enabled = true;
                mediumPot.GetComponent<Renderer>().enabled = false;
                largePot.GetComponent<Renderer>().enabled = false;
            }
            else if (depth < 8)
            {
                pot = 2;
                smallPot.GetComponent<Renderer>().enabled = false;
                mediumPot.GetComponent<Renderer>().enabled = true;
                largePot.GetComponent<Renderer>().enabled = false;
            }
            else
            {
                pot = 3;
                smallPot.GetComponent<Renderer>().enabled = false;
                mediumPot.GetComponent<Renderer>().enabled = false;
                largePot.GetComponent<Renderer>().enabled = true;
            }
            // Code for testing purposes
            Console.WriteLine("\nPot: " + pot);
        }
    }

    public class Internode
    {
        public float Thickness; //thickness
        public float Length; //length

        public string ID; // ID/name of the current internode
        public int Depth;
        public int Angle; // 0 to 45, angle of internode in z-axis
        public int Rotation; // -180 to 180, angle of internode in x/y-axis
        public string PetioleId; // ID for the petiole of this internode, which is the current internode's ID + 'p' for 'p'etiole
        public string InternodeId; // ID for the next internode following this internode, '' if no more internodes.

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

            // Generate measurements for current internode
            Length = 0.15f - (0.02f * lightPower);
            Thickness = (0.005f + (0.005f * age))  * (0.75f + lightPower * 0.05f);
            Angle = Start.Rand(0, 45);
            Rotation = Start.Rand(-180, 180);

            PetioleId = ID + "p";
            petioles.Add(
                PetioleId,
                new Petiole(
                    age,
                    lightPower,
                    PetioleId,
                    depth,
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
            // Else this internode is top-internode and no more internodes are coming
            else
            {
                InternodeId = "";
                petioles[ID + "p"].Angle = 90;
            }
        }
    }

    public class Petiole
    {
        public float Length;
        public float ThicknessStart;
        public float ThicknessEnd;
        public float WidthEnd;
        public int Angle; // 15 to 60, angle of petiole in z-axis
        public int Rotation; // 20 to 110 or 250 to 340, angle of petiole in x/y-axis

        public string ID; // ID/name of the current petiole
        public int Depth;
        public string LeafId; // ID of leaf connected to current petiole, ID of petiole plus 'l' for 'l'eaf

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

            // Generate measurements for current petiole
            ThicknessStart = (0.005f + (0.005f * age))  * (0.75f + lightPower * 0.05f);
            ThicknessEnd = 0.003f + 0.001f * age;
            Length = 0.1625f + (0.0875f * age) - (Depth * 0.025f);
            Angle = Start.Rand(15, 60);
            Rotation = Start.Rand(135, 260);
            if (depth % 2 == 0)
            {
                Rotation += 145;
            }
            LeafId = ID + "l";
            leaves.Add(
                LeafId,
                new Leaf(
                    age,
                    lightPower,
                    LeafId,
                    depth,
                    Angle,
                    Rotation,
                    ref internodes,
                    ref petioles,
                    ref leaves
                )
            );
            // int LeafModelId = leaves[LeafId].LeafModelId;
            // WidthEnd = ThicknessEnd - 0.0028f + 0.0014f * LeafModelId;
        }
    }

    public class Leaf
    {
        public float[] Holes; //10 different 0-1 hole size (0-100%)

        /* Guide for which index corresponds to which hole in the leaf mesj
             ||
        0(         )1
         2(       )3
          4(     )5
           6(   )7
            8( )9
        */
        public float ThicknessFenestrations; // thickness of fenestrations 0-1 thickness (0-100%)
        public float LengthFenestrations; // length of fenestrations 0-1 length (0-100%)
        public float Width; // width of whole leaf
        public float Height; // height of whole leaf

        public string ID; // ID/name of the current leaf
        public int Depth;
        public int Angle; // Same as for petiole
        public int Rotation; // Same as for petiole

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

            // Generate values for leaf measurements
            Width = (0.0875f + (age * 0.0625f) - (Depth * 0.025f)) * (0.75f + lightPower * 0.05f);
            Height = 1.1f * Width;

            Holes = new float[10];
            float hole = Width / 0.40f * 0.85f;
            if (Width < 0.3f)
            {
                hole = Width / 0.30f;
            }
            if (Width < 0.2f)
            {
                hole = Width / 0.20f;
            }
            for (int i = 2; i < 10; i++)
            {
                Holes[i] = hole - (i * 0.08f);
                if (Holes[i] < 0.5f * hole)
                {
                    Holes[i] = 0;
                }
            }
            int reverseDepth = Convert.ToUInt16(ID[0]) - Convert.ToUInt16('a') + 1;
            /*
            ID       Depth    reverseDepth    percentOfPlant
            --------------------------------------------------
            kpl        1        11               1.0
            jpl        2        10               0.91
            ipl        3        9                0.82
            hpl        4        8                0.73
            gpl        5        7                0.64
            fpl        6        6                0.55
            epl        7        5                0.45
            dpl        8        4                0.36
            cpl        9        3                0.27
            bpl        10       2                0.18
            apl        11       1                0.09
            
            fpl        1        6                1.0
            epl        2        5                0.83
            dpl        3        4                0.67
            cpl        4        3                0.5
            bpl        5        2                0.33
            apl        6        1                0.17

            cpl        1        3                1.0
            bpl        2        2                0.67
            apl        3        1                0.33
            */
            int highestDepth = Depth + reverseDepth - 1;
            float percentOfPlant = ((float)reverseDepth) / ((float)highestDepth);

            ThicknessFenestrations = hole  * (0.75f + lightPower * 0.05f);
            LengthFenestrations = hole / 10 * reverseDepth * 0.60f;
            if (
                ThicknessFenestrations < 0.3f
                || LengthFenestrations < 0.0f
                || percentOfPlant < 0.34f
            )
            {
                ThicknessFenestrations = 0.0f;
                LengthFenestrations = 0.0f;
            }
        }
    }
}
