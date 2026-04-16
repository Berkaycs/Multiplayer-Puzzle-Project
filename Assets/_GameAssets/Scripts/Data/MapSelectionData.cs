using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapSelectionData", menuName = "ScriptableObjects/MapSelectionData", order = 1)]
public class MapSelectionData : ScriptableObject
{
    public List<MapInfo> Maps;
}

[Serializable]
public struct MapInfo
{
    public Color MapThumbnail;
    public string MapName;
    public string SceneName;
}
