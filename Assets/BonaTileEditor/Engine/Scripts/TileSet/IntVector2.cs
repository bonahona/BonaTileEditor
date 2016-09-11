using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class IntVector2
{
    public int X { get; set; }
    public int Y { get; set; }

    public IntVector2()
    {
        X = 0;
        Y = 0;
    }

    public IntVector2(IntVector2 other)
    {
        X = other.X;
        Y = other.Y;
    }

    public IntVector2(int x, int y)
    {
        X = x;
        Y = y;
    }

}