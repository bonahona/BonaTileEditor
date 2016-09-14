using UnityEngine;
using System.Collections;

public class MapSegmentBlock
{
    public int Width { get; set; }
    public int Height { get; set; }
    public Point[,] SelectedBlocks { get; set; }

    public MapSegmentBlock()
    {
        Width = 0;
        Height = 0;
        SelectedBlocks = new Point[0, 0];
    }

    public MapSegmentBlock(int x, int y) : this(new Point(x, y))
    {
    }

    public MapSegmentBlock(Point point)
    {
        Width = 1;
        Height = 1;
        SelectedBlocks = new Point[1, 1];
        SelectedBlocks[0, 0] = point;
    }
}
