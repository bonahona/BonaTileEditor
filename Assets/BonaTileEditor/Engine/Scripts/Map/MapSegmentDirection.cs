using UnityEngine;
using System.Collections;

[System.Flags]
public enum MapSegmentDirection : int
{
    None    = 0,
    Up      = 1,
    Down    = 2,
    Left    = 4,
    Right   = 8,
    All     = 15
}
