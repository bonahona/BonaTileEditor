using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[CreateAssetMenu( fileName = "New Tileset", menuName = "Bona Tile Editor/Tileset")]
public class TileSet : ScriptableObject
{
    public List<TileSetLayer> Layers;

    public TileSet()
    {
        Layers = new List<TileSetLayer>();
    }
}