using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public struct Point
{
    public static readonly Point Zero = new Point { X = 0, Y = 0 };

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

    public static Point operator+(Point lhs, Point rhs)
    {
        var result = new Point(lhs.X + rhs.X, lhs.Y + rhs.Y);
        return result;
    }

    public static Point operator -(Point lhs, Point rhs)
    {
        var result = new Point(lhs.X - rhs.X, lhs.Y - rhs.Y);
        return result;
    }

    public override int GetHashCode()
    {
        return Y * 1000 + Y;
    }

    public IntVector2 ToInt2Vector()
    {
        return new IntVector2 { X = X, Y = Y };
    }

    public Vector2 ToVector2()
    {
        return new Vector2(X, Y);
    }

    public override String ToString()
    {
        return String.Format("({0}, {1})", X, Y);
    }
}