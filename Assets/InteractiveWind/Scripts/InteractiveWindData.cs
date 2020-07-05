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


[CreateAssetMenu(fileName = "WindSystemData", menuName = "InteractiveWind/WindSystemData")]
[System.Serializable]
public class WindSystemData : ScriptableObject
{
    public enum ADVECTION { NORMAL = 1, BFECC = 2, MACCORMACK = 3 };
	public ADVECTION m_advectionType = ADVECTION.NORMAL;
	public int m_width = 128;
	public int m_height = 128;
	public int m_depth = 128;
	public int m_iterations = 10;
	public float m_vorticityStrength = 1.0f;
	public float m_densityAmount = 1.0f;
	public float m_densityDissipation = 0.999f;
	public float m_densityBuoyancy = 1.0f;
	public float m_densityWeight = 0.0125f;
	public float m_temperatureAmount = 10.0f;
	public float m_temperatureDissipation = 0.995f;
	public float m_velocityDissipation = 0.995f;
	public float m_inputRadius = 0.04f;
	public Vector4 m_inputPos = new Vector4(0.5f, 0.1f, 0.5f, 0.0f);
	public float m_ambientTemperature = 0.0f;
}