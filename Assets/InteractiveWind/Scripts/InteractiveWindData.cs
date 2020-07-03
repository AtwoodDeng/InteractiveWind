using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class InteractiveWindData : ScriptableObject
//{
//    public List<WindDataLOD> windDataLODs = new List<WindDataLOD>();
//}

[CreateAssetMenu(fileName = "WindLODData", menuName = "InteractiveWind/WindLODData")]
[System.Serializable]
public class WindLODData : ScriptableObject
{
    public Vector3 WindAreaSize = new Vector3(1f, 1f, 1f);
    public Vector3 WindAreaSizeInv
    {
        get
        {
            return new Vector3(1f / WindAreaSize.x, 1f / WindAreaSize.y, 1f / WindAreaSize.z);
        }
    }
    public float WindResolution = 1f;
}