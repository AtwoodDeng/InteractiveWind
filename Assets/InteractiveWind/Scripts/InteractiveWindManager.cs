using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;


[ExecuteInEditMode]
public class InteractiveWindManager : MonoBehaviour
{

    public static InteractiveWindManager Instance
    {
        get
        {
            if (m_Instance == null)
                m_Instance = FindObjectOfType<InteractiveWindManager>();
            return m_Instance;

        }
    }
    private static InteractiveWindManager m_Instance;
    

    static public int THREAD_NUM = 8;

    public ComputeShader FinalizeShader;

    // public VisualEffect target;
    static public Vector3 Center { get { return Instance == null ? new Vector3(0,0,0): Instance.transform.position;  } }

    [System.Serializable]
    public class WindLODDataRT
    {
        public RenderTexture[] WindDataTex;
        public WindLODData windData;
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public int depth;
    }
    public List<WindLODDataRT> windDataLODs = new List<WindLODDataRT>();
    public static List<VisualEffect> vfxList = new List<VisualEffect>();

    public static int WindDataTexID = Shader.PropertyToID("WindDataTex");
    public static int WindAreaSizeID = Shader.PropertyToID("_WindAreaSize");
    public static int WindAreaSizeInvID = Shader.PropertyToID("_WindAreaSizeInv");
    public static int WindResolutionID = Shader.PropertyToID("_WindResolution");
    public static int WindDataTexSizeID = Shader.PropertyToID("_WindDataTexSizeID");
    public static int CenterID  = Shader.PropertyToID("_Center");


    public static void Register(VisualEffect vfx)
    {
        if (vfx != null)
            vfxList.Add(vfx);
    }

    public static void Unregister(VisualEffect vfx)
    {
        if (vfxList.Contains(vfx))
            vfxList.Remove(vfx);
    }

    public bool IsReady()
    {
        if (windDataLODs == null)
            return false;

        for ( int i = 0; i < windDataLODs.Count; ++ i )
        {
            if (windDataLODs[i].windData == null)
                return false;
        }

        return true;
    }

    private void Update()
    {
        UpdateWindData();
        // ComputeWindData();
        // ExportWind();
        for (int i = 0; i < vfxList.Count; ++i)
            ExportWindToVFX(vfxList[i], windDataLODs[0]);
    }


    public void OnEnable()
    {
        ReleaseRT();

        UpdateWindData();
    }

    public void OnDisable()
    {
        ReleaseRT();
    }

    void ReleaseRT()
    {
        for (int i = 0; i < windDataLODs.Count; ++i)
        {
            var data = windDataLODs[i];
            if (data.WindDataTex != null)
            {
                for( int j = 0; j < data.WindDataTex.Length; ++j)
                {
                    if (data.WindDataTex[j] != null )
                        data.WindDataTex[j].Release();
                    data.WindDataTex[j] = null;
                }
            }
        }
    }


    // =========================== Set up Wind Data ===========================

    public void UpdateWindData()
    {
        if (!IsReady())
            return;

        for (int i = 0; i < windDataLODs.Count; ++i)
        {
            if ( CheckWindData(windDataLODs[i]))
                InitalizeWindDataLOD(windDataLODs[i]);
        }
    }

    public bool CheckWindData(WindLODDataRT data )
    {
        if (data.windData == null)
            return false;

        var width = Mathf.CeilToInt(data.windData.WindAreaSize.x / data.windData.WindResolution);
        var height = Mathf.CeilToInt(data.windData.WindAreaSize.y / data.windData.WindResolution);
        var depth = Mathf.CeilToInt(data.windData.WindAreaSize.z / data.windData.WindResolution);

        return (data.WindDataTex == null || data.WindDataTex.Length != 2 || data.WindDataTex[0] == null || data.WindDataTex[1] == null
            || data.WindDataTex[0].width != width || data.WindDataTex[0].height != height || data.WindDataTex[0].volumeDepth != depth 
            || data.WindDataTex[0].width != width || data.WindDataTex[0].height != height || data.WindDataTex[0].volumeDepth != depth
            );
    }

    public void InitalizeWindDataLOD(WindLODDataRT data )
    {
        if (data.windData == null)
            return;

        var width = Mathf.CeilToInt(data.windData.WindAreaSize.x / data.windData.WindResolution);
        var height = Mathf.CeilToInt(data.windData.WindAreaSize.y / data.windData.WindResolution);
        var depth = Mathf.CeilToInt(data.windData.WindAreaSize.z / data.windData.WindResolution);

        if (data.WindDataTex == null || data.WindDataTex.Length != 2 )
            data.WindDataTex = new RenderTexture[2];
        if (data.WindDataTex[0] != null)
            data.WindDataTex[0].Release();
        if (data.WindDataTex[1] != null)
            data.WindDataTex[1].Release();
        data.WindDataTex[0] = CreateRT3D(width, height, depth);
        data.WindDataTex[1] = CreateRT3D(width, height, depth);

        data.width = width;
        data.height = height;
        data.depth = depth;

    }

    public RenderTexture CreateRT3D( int width, int height, int depth )
    {
        var rt = new RenderTexture(width,height ,0,RenderTextureFormat.ARGB32,0);
        rt.antiAliasing = 1;
        rt.filterMode = FilterMode.Trilinear;
        rt.enableRandomWrite = true;
        rt.volumeDepth = depth;
        rt.dimension = TextureDimension.Tex3D;
        rt.Create();

        return rt;
    }

    // ========================================================================================

    // ================================ Compute Wind Data =====================================
   
    static public void AssignBasicMaterial( ComputeShader shader , WindLODDataRT data )
    {
        shader.SetVector(WindAreaSizeID, data.windData.WindAreaSize);
        shader.SetVector(WindAreaSizeInvID, data.windData.WindAreaSizeInv);
        shader.SetFloat(WindResolutionID, data.windData.WindResolution);
        shader.SetInts(WindDataTexSizeID, new int[] { data.width, data.height, data.depth });
    }

    
    public void ComputeWindData()
    {
        // Finalize
        for (int i = 0; i < windDataLODs.Count; ++i)
        {
            var data = windDataLODs[i];
            AssignBasicMaterial(FinalizeShader, data);

            int kernal = FinalizeShader.FindKernel("CSMain");
            FinalizeShader.SetTexture(kernal, WindDataTexID, data.WindDataTex[0]);

            FinalizeShader.Dispatch(kernal, Mathf.CeilToInt(data.width / THREAD_NUM),
                Mathf.CeilToInt(data.height / THREAD_NUM),
                Mathf.CeilToInt(data.depth / THREAD_NUM));
        }
    }

    // ========================================================================================

    // ================================ Output ================================================
    static public void ExportWindToVFX ( VisualEffect target , WindLODDataRT data )
    {
 
        target.SetVector3(WindAreaSizeInvID, data.windData.WindAreaSizeInv);
        target.SetVector3(WindAreaSizeID, data.windData.WindAreaSize);
        target.SetTexture(WindDataTexID, data.WindDataTex[0]);
        target.SetVector3(CenterID, InteractiveWindManager.Center);
    }

    public void OnDrawGizmos()
    {
        if (!IsReady())
            return;

        var pos = transform.position;
        for ( int i = 0; i < windDataLODs.Count; ++ i )
        {
            Gizmos.color = Color.HSVToRGB(i * 0.13f + 0.1f, 1f, 1f);
            Gizmos.DrawWireCube(pos , windDataLODs[i].windData.WindAreaSize);
        }
    }
}
