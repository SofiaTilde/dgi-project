using JetBrains.Annotations;
using UnityEngine;

public class Classes { }

//todo id values
public class Leaf
{
    public (int, int) Holes;
    public (int, int) Size; //width, height
    public Color MainColor;
    public Color VeinColor;
    public int ID;
    public int Depth;

    public Leaf(int age, int lightPower, int id, int depth)
    {
        ID = id;
        Depth = depth;
        if (depth == 0)
        {
            //lighter color
        }
    }
}

public class Internode
{
    public (int, int) Size; //thickness, length
    public Color MainColor;
    public int ID;
    public int Depth;
    public Petiole petiole;
    public Internode internode;

    public Internode(int age, int lightPower, int id, int depth)
    {
        ID = id;
        Depth = depth;
        petiole = new Petiole(age, lightPower, id + 1, depth - 1);
        if (depth != 0)
        {
            internode = new Internode(age, lightPower, id + 1, depth - 1);
        }
    }
}

public class Petiole
{
    public int Length;
    public (int, int) Thickness; //start and end thickness
    public int Angle; //-45 to 45
    public int Rotation; //-180 to 180
    public Color MainColor;
    public int ID;
    public int Depth;
    public Leaf Leaf;

    public Petiole(int age, int lightPower, int id, int depth)
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
        Leaf = new Leaf(age, lightPower, id + 1, depth - 1);
    }
}
