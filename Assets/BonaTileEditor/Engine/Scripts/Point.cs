using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public struct Point
{
    public int X;
    public int Y;

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Point(IntVector2 vector)
    {
        X = vector.X;
        Y = vector.Y;
    }

    public static bool operator==(Point lhs, Point rhs)
    {
        return (lhs.X == rhs.X && lhs.Y == rhs.Y);
    }

    public override bool Equals(object obj)
    {
        return (this == (Point)obj);
    }

    public static bool operator !=(Point lhs, Point rhs)
    {
        return (lhs.X != rhs.X || lhs.Y != rhs.Y);
    }

    public override int GetHashCode()
    {
        return Y * 1000 + Y;
    }
}