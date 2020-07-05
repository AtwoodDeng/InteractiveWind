using Sirenix.OdinInspector;
using System;
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
    

    static public int NUM_THREADS = 8;
    const int READ = 0;
    const int WRITE = 1;
    const int PHI_N_HAT = 0;
    const int PHI_N_1_HAT = 1;

    //You can change this or even use Time.DeltaTime but large time steps can cause numerical errors
    const float TIME_STEP = 0.1f;

    public ComputeShader FinalizeShader;

    // public VisualEffect target;
    static public Vector3 Center { get { return Instance == null ? new Vector3(0,0,0): Instance.transform.position;  } }

    [System.Serializable]
    public class WindDataRuntime
    {
        public RenderTexture[] WindDataTex;
        public WindLODData basicData;
        public WindSystemData systemData;
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public int depth;
        public Vector3 size { get { return new Vector4(width, height, depth, 0); } }

        [HideInInspector] public ComputeBuffer[] m_density, m_velocity, m_pressure, m_temperature, m_phi;
        [HideInInspector] public ComputeBuffer m_temp3f, m_obstacles;

    }


    public List<WindDataRuntime> windDataLODs = new List<WindDataRuntime>();
    public static List<VisualEffect> vfxList = new List<VisualEffect>();


    public static int WindDataTexID             = Shader.PropertyToID("WindDataTex");
    public static int WindAreaSizeID            = Shader.PropertyToID("_WindAreaSize");
    public static int WindAreaSizeInvID         = Shader.PropertyToID("_WindAreaSizeInv");
    public static int WindResolutionID          = Shader.PropertyToID("_WindResolution");
    public static int WindDataTexSizeID         = Shader.PropertyToID("_WindDataTexSizeID");
    public static int CenterID                  = Shader.PropertyToID("_Center");

    public static int SizeID                    = Shader.PropertyToID("_Size");
    public static int RadiusID                  = Shader.PropertyToID("_Radius");
    public static int AmountID                  = Shader.PropertyToID("_Amount");
    public static int DeltaTimeID               = Shader.PropertyToID("_DeltaTime");
    public static int PosID                     = Shader.PropertyToID("_Pos");
    public static int UpID                      = Shader.PropertyToID("_Up");
    public static int BuoyancyID                = Shader.PropertyToID("_Buoyancy");
    public static int AmbientTemperatureID      = Shader.PropertyToID("_AmbientTemperature");
    public static int WeightID                  = Shader.PropertyToID("_Weight");
    public static int DissipateID               = Shader.PropertyToID("_Dissipate");
    public static int ForwardID                 = Shader.PropertyToID("_Forward");
    public static int DecayID                   = Shader.PropertyToID("_Decay");
    public static int EpsilonID                 = Shader.PropertyToID("_Epsilon");



    public static int ReadID                    = Shader.PropertyToID("_Read");
    public static int WriteID                   = Shader.PropertyToID("_Write");
    public static int Read3fID                  = Shader.PropertyToID("_Read3f");
    public static int Write3fID                 = Shader.PropertyToID("_Write3f");
    public static int Read1fID                  = Shader.PropertyToID("_Read1f");
    public static int Write1fID                 = Shader.PropertyToID("_Write1f");
    public static int DensityID                 = Shader.PropertyToID("_Density");
    public static int VelocityID                = Shader.PropertyToID("_Velocity");
    public static int ObstaclesID               = Shader.PropertyToID("_Obstacles");
    public static int TemperatureID             = Shader.PropertyToID("_Temperature");
    public static int VorticityID               = Shader.PropertyToID("_Vorticity");
    public static int PressureID                = Shader.PropertyToID("_Pressure");
    public static int DivergenceID              = Shader.PropertyToID("_Divergence");
    public static int Phi_n_hatID               = Shader.PropertyToID("_Phi_n_hat");
    public static int Phi_n_1_hatID             = Shader.PropertyToID("_Phi_n_1_hat");


    [Button]
    void RefreshAllData()
    {
        ReleaseAllData();

        UpdateWindData(true);
    }

    struct F3
    {
        float x;
        float y;
        float z;
        public string ToString(string format)
        {
            return x.ToString(format) + "," + y.ToString(format) + "," + z.ToString(format);
        }
    }

    [Button]
    void DebugPrint()
    {

        var data = windDataLODs[0];

        if (data.width > 10 || data.height > 10 || data.depth > 10) 
            return;

        float[] obstacles = new float[data.m_obstacles.count];
        float[] density = new float[data.m_density[READ].count];
        float[] temperature = new float[data.m_temperature[READ].count];
        F3[] velocity = new F3[data.m_velocity[READ].count];
        data.m_obstacles.GetData(obstacles);
        data.m_density[READ].GetData(density);
        data.m_temperature[READ].GetData(temperature);
        data.m_velocity[READ].GetData(velocity);

        string res = "";
        for ( int i = 0; i < obstacles.Length; ++ i )
        {
            res += obstacles[i].ToString("F2") + " ";
            if ( ( i + 1 ) % data.width == 0)
                res += "\n";
            if ((i + 1) % (data.width * data.height) == 0)
                res += "\n";
        }
        Debug.Log( "Obstacles :\n" + res);

        res = "";
        for (int i = 0; i < density.Length; ++i)
        {
            res += density[i].ToString("F2") + " ";
            if ((i + 1) % data.width == 0)
                res += "\n";
            if ((i + 1) % ( data.width * data.height ) == 0)
                res += "\n";
        }
        Debug.Log("Density :\n" + res);

        res = "";
        for (int i = 0; i < temperature.Length; ++i)
        {
            res += temperature[i].ToString("F2") + " ";
            if ((i + 1) % data.width == 0)
                res += "\n";
            if ((i + 1) % (data.width * data.height) == 0)
                res += "\n";
        }
        Debug.Log("Temperature :\n" + res);

        res = "";
        for (int i = 0; i < velocity.Length; ++i)
        {
            res += velocity[i].ToString("F2") + " ";
            if ((i + 1) % data.width == 0)
                res += "\n";
            if ((i + 1) % (data.width * data.height) == 0)
                res += "\n";
        }
        Debug.Log("Velocity :\n" + res);
    }

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
            if (windDataLODs[i].basicData == null)
                return false;
            if (windDataLODs[i].systemData  == null)
                return false;
        }

        return true;
    }

    #region BASIC_FUNCTION

    private void Update()
    {
        UpdateWindData();
        // ComputeWindData();
        // ExportWind();
        if (IsReady())
        {
            for (int i = 0; i < vfxList.Count; ++i)
                ExportWindToVFX(vfxList[i], windDataLODs[0]);
        }

    }


    public void OnEnable()
    {
        ReleaseAllData();

        UpdateWindData(true);
    }

    public void OnDisable()
    {
        ReleaseAllData();
    }

    #endregion

    #region DATA_LIFE_CYCLE

    void ReleaseAllData()
    {
        for (int i = 0; i < windDataLODs.Count; ++i)
        {
            var data = windDataLODs[i];
            ReleaseRT(data);
            ReleaseComputeBuffer(data);
        }
    }
    void ReleaseRT(WindDataRuntime data)
    {
        if (data.WindDataTex != null)
        {
            for (int j = 0; j < data.WindDataTex.Length; ++j)
            {
                if (data.WindDataTex[j] != null)
                    data.WindDataTex[j].Release();
                data.WindDataTex[j] = null;
            }
        }
    }


    void ReleaseComputeBuffer(WindDataRuntime data)
    {
        if (data.m_density != null)
        {
            for (int i = 0; i < data.m_density.Length; ++i)
            {
                if (data.m_density[i] != null)
                    data.m_density[i].Release();
                data.m_density[i] = null;
            }
        }
        if (data.m_velocity != null)
        {
            for (int i = 0; i < data.m_velocity.Length; ++i)
            {
                if (data.m_velocity[i] != null)
                    data.m_velocity[i].Release();
                data.m_velocity[i] = null;
            }
        }
        if (data.m_pressure != null)
        {
            for (int i = 0; i < data.m_pressure.Length; ++i)
            {
                if (data.m_pressure[i] != null)
                    data.m_pressure[i].Release();
                data.m_pressure[i] = null;
            }
        }
        if (data.m_temperature != null)
        {
            for (int i = 0; i < data.m_temperature.Length; ++i)
            {
                if (data.m_temperature[i] != null)
                    data.m_temperature[i].Release();
                data.m_temperature[i] = null;
            }
        }
        if (data.m_temperature != null)
        {
            for (int i = 0; i < data.m_phi.Length; ++i)
            {
                if (data.m_phi[i] != null)
                    data.m_phi[i].Release();
                data.m_phi[i] = null;
            }
        }
        if (data.m_temp3f != null)
        {
            data.m_temp3f.Release();
            data.m_temp3f = null;
        }
        if (data.m_obstacles != null)
        {
            data.m_obstacles.Release();
            data.m_obstacles = null;
        }
    }



    #endregion



    // =========================== Set up Wind Data ===========================

    public void UpdateWindData( bool forceInit = false)
    {
        if (!IsReady())
            return;

        for (int i = 0; i < windDataLODs.Count; ++i)
        {
            if (forceInit || CheckWindData(windDataLODs[i]))
                InitalizeWindDataLOD(windDataLODs[i]);
        }
    }

    public bool CheckWindData(WindDataRuntime data )
    {
        if (data.basicData == null)
            return false;

        var width = Mathf.CeilToInt(data.basicData.WindAreaSize.x / data.basicData.WindResolution);
        var height = Mathf.CeilToInt(data.basicData.WindAreaSize.y / data.basicData.WindResolution);
        var depth = Mathf.CeilToInt(data.basicData.WindAreaSize.z / data.basicData.WindResolution);

        return data.width != width || data.height != height || data.depth != depth;
    }

    public void CreateComputeBufferFloat( ref ComputeBuffer[] cbs , int size , bool isFloat3 = false , float initValue = 0)
    {
        if (cbs == null || cbs.Length != 2)
            cbs = new ComputeBuffer[2];
        if (cbs[0] != null)
            cbs[0].Release();
        if (cbs[1] != null)
            cbs[1].Release();
        cbs[0] = new ComputeBuffer(size, sizeof(float) * (isFloat3? 3:1) );
        cbs[1] = new ComputeBuffer(size, sizeof(float) * (isFloat3 ? 3 : 1));

        if (!isFloat3)
        {
            var data = new float[size];
            for (int i = 0; i < data.Length; ++i) data[i] = initValue; 
            cbs[0].SetData(data);
        }
    }

    public void CreateRT(ref RenderTexture[] rts, int width, int height, int depth)
    {
        if (rts == null || rts.Length != 2)
            rts = new RenderTexture[2];
        if (rts[0] != null)
            rts[0].Release();
        if (rts[1] != null)
            rts[1].Release();
        rts[0] = CreateRT3D(width, height, depth);
        rts[1] = CreateRT3D(width, height, depth);
    }

    public void InitalizeWindDataLOD(WindDataRuntime data )
    {
        if (data.basicData == null)
            return;

        var width = Mathf.CeilToInt(data.basicData.WindAreaSize.x / data.basicData.WindResolution);
        var height = Mathf.CeilToInt(data.basicData.WindAreaSize.y / data.basicData.WindResolution);
        var depth = Mathf.CeilToInt(data.basicData.WindAreaSize.z / data.basicData.WindResolution);

        data.width = width;
        data.height = height;
        data.depth = depth;

        CreateRT(ref data.WindDataTex, width, height, depth);

        int totalSize = width * height * depth;
        CreateComputeBufferFloat(ref data.m_density , totalSize );
        CreateComputeBufferFloat(ref data.m_temperature, totalSize);
        CreateComputeBufferFloat(ref data.m_phi, totalSize);
        CreateComputeBufferFloat(ref data.m_velocity, totalSize , true );
        CreateComputeBufferFloat(ref data.m_pressure, totalSize);

        if (data.m_obstacles != null)
            data.m_obstacles.Release();
        data.m_obstacles = new ComputeBuffer(totalSize, sizeof(float));
        if (data.m_temp3f != null)
            data.m_temp3f.Release();
        data.m_temp3f = new ComputeBuffer(totalSize, sizeof(float)*3);

        //ComputeObstacles();
    }

    public RenderTexture CreateRT3D( int width, int height, int depth )
    {
        var rt = new RenderTexture(width,height ,0,RenderTextureFormat.ARGBFloat,0);
        rt.antiAliasing = 1;
        rt.filterMode = FilterMode.Trilinear;
        rt.enableRandomWrite = true;
        rt.volumeDepth = depth;
        rt.dimension = TextureDimension.Tex3D;
        rt.Create();

        return rt;
    }

    //void ComputeObstacles()
    //{
    //    if (m_computeObstacles != null)
    //    {
    //        for (int i = 0; i < windDataLODs.Count; ++i)
    //        {
    //            var data = windDataLODs[i];

    //            int kernel = 0;
    //            m_computeObstacles.SetVector("_Size", data.size);
    //            m_computeObstacles.SetBuffer(0, "_Write", data.m_obstacles);

    //            m_computeObstacles.Dispatch(
    //                kernel,
    //                Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
    //                Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
    //                Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));
    //        }
    //    }
    //}

    // ========================================================================================

    // ================================ Compute Wind Data =====================================

    #region ASSIGN_MATERAIAL
    static public void AssignBasicMaterial( ComputeShader shader , WindDataRuntime data )
    {
        shader.SetVector(WindAreaSizeID, data.basicData.WindAreaSize);
        shader.SetVector(WindAreaSizeInvID, data.basicData.WindAreaSizeInv);
        shader.SetFloat(WindResolutionID, data.basicData.WindResolution);
        shader.SetInts(WindDataTexSizeID, new int[] { data.width, data.height, data.depth });
    }


    #endregion

    static private void Swap(ComputeBuffer[] buffer)
    {
        ComputeBuffer tmp = buffer[READ];
        buffer[READ] = buffer[WRITE];
        buffer[WRITE] = tmp;
    }

    static private void Swap(RenderTexture[] rts)
    {
        RenderTexture tmp = rts[READ];
        rts[READ] = rts[WRITE];
        rts[WRITE] = tmp;
    }

    // m_computeObstacles
    static public void ComputeObstacles( CommandBuffer cmd, ComputeShader shader, WindDataRuntime data )
    {
        int kernel = 0;
        cmd.SetComputeVectorParam(shader, SizeID, data.size);
        cmd.SetComputeBufferParam(shader, kernel , WriteID, data.m_obstacles);

        cmd.DispatchCompute(shader,
            kernel,
            Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));
    }

    // m_applyImpulse
    static public void ApplyImpulse(CommandBuffer cmd, ComputeShader shader, WindDataRuntime data, float dt, float amount, ComputeBuffer[] buffer)
    {
        int kernel = 0;

        cmd.SetComputeVectorParam(shader, SizeID, data.size);
        cmd.SetComputeFloatParam(shader, RadiusID, data.systemData.m_inputRadius);
        cmd.SetComputeFloatParam(shader, AmountID, amount);
        cmd.SetComputeFloatParam(shader, DeltaTimeID, dt);
        cmd.SetComputeVectorParam(shader, PosID, data.systemData.m_inputPos);

        cmd.SetComputeBufferParam(shader, kernel, ReadID, buffer[READ]);
        cmd.SetComputeBufferParam(shader, kernel, WriteID, buffer[WRITE]);

        cmd.DispatchCompute(shader,
            kernel,
            Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));

        Swap(buffer);
    }

    // m_applyBuoyancy
    static public void ApplyBuoyancy(CommandBuffer cmd, ComputeShader shader, WindDataRuntime data, float dt)
    {
        int kernel = 0;

        cmd.SetComputeVectorParam(shader, SizeID, data.size);
        cmd.SetComputeVectorParam(shader, UpID, new Vector4(0, 1, 0, 0));
        cmd.SetComputeFloatParam(shader, BuoyancyID, data.systemData.m_densityBuoyancy);
        cmd.SetComputeFloatParam(shader, AmbientTemperatureID, data.systemData.m_ambientTemperature);
        cmd.SetComputeFloatParam(shader, WeightID, data.systemData.m_densityWeight);
        cmd.SetComputeFloatParam(shader, DeltaTimeID, dt);


        cmd.SetComputeBufferParam(shader, kernel, WriteID, data.m_velocity[WRITE]);
        cmd.SetComputeBufferParam(shader, kernel, VelocityID, data.m_velocity[READ]);
        cmd.SetComputeBufferParam(shader, kernel, DensityID, data.m_density[READ]);
        cmd.SetComputeBufferParam(shader, kernel, TemperatureID, data.m_temperature[READ]);

        cmd.DispatchCompute(shader,
            kernel,
            Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));

        Swap(data.m_velocity);
    }

    // m_applyAdvect
    static public void ApplyAdvection(CommandBuffer cmd, ComputeShader shader , WindDataRuntime data, float dt, float dissipation, float decay, ComputeBuffer[] buffer, float forward = 1.0f)
    {
        int kernel = (int)WindSystemData.ADVECTION.NORMAL;
        cmd.SetComputeVectorParam(shader, SizeID, data.size);
        cmd.SetComputeFloatParam(shader, DeltaTimeID, dt);
        cmd.SetComputeFloatParam(shader, DissipateID, dissipation);
        cmd.SetComputeFloatParam(shader, ForwardID, forward);
        cmd.SetComputeFloatParam(shader, DecayID, decay);


        cmd.SetComputeBufferParam(shader, kernel, Read1fID, buffer[READ]);
        cmd.SetComputeBufferParam(shader, kernel, Write1fID, buffer[WRITE]);
        cmd.SetComputeBufferParam(shader, kernel, VelocityID, data.m_velocity[READ]);
        cmd.SetComputeBufferParam(shader, kernel, ObstaclesID, data.m_obstacles);

        cmd.DispatchCompute(shader,
            kernel,
            Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));

        Swap(buffer);
    }

    // m_applyAdvect
    static public void ApplyAdvection(CommandBuffer cmd, ComputeShader shader, WindDataRuntime data, float dt, float dissipation, float decay, ComputeBuffer read, ComputeBuffer write, float forward = 1.0f)
    {
        int kernel = (int)WindSystemData.ADVECTION.NORMAL;
        cmd.SetComputeVectorParam(shader, SizeID, data.size);
        cmd.SetComputeFloatParam(shader, DeltaTimeID, dt);
        cmd.SetComputeFloatParam(shader, DissipateID, dissipation);
        cmd.SetComputeFloatParam(shader, ForwardID, forward);
        cmd.SetComputeFloatParam(shader, DecayID, decay);


        cmd.SetComputeBufferParam(shader, kernel, Read1fID, read);
        cmd.SetComputeBufferParam(shader, kernel, Write1fID, write);
        cmd.SetComputeBufferParam(shader, kernel, VelocityID, data.m_velocity[READ]);
        cmd.SetComputeBufferParam(shader, kernel, ObstaclesID, data.m_obstacles);

        cmd.DispatchCompute(shader,
            kernel,
            Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));
    }

    // m_applyAdvect
    static public void ApplyAdvectionVelocity(CommandBuffer cmd, ComputeShader shader, WindDataRuntime data, float dt)
    {
        int kernel = 0;
        cmd.SetComputeVectorParam(shader, SizeID, data.size);
        cmd.SetComputeFloatParam(shader, DeltaTimeID, dt);
        cmd.SetComputeFloatParam(shader, DissipateID, data.systemData.m_velocityDissipation);
        cmd.SetComputeFloatParam(shader, ForwardID, 1.0f);
        cmd.SetComputeFloatParam(shader, DecayID, 0.0f);


        cmd.SetComputeBufferParam(shader, kernel, Read3fID, data.m_velocity[READ]);
        cmd.SetComputeBufferParam(shader, kernel, Write3fID, data.m_velocity[WRITE]);
        cmd.SetComputeBufferParam(shader, kernel, VelocityID, data.m_velocity[READ]);
        cmd.SetComputeBufferParam(shader, kernel, ObstaclesID, data.m_obstacles);

        cmd.DispatchCompute(shader,
            kernel,
            Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));

        Swap(data.m_velocity);
    }

    // m_computeVorticity
    static public void ComputeVorticity(CommandBuffer cmd, ComputeShader shader, WindDataRuntime data, float dt)
    {
        int kernel = 0;
        cmd.SetComputeVectorParam(shader, SizeID, data.size);

        cmd.SetComputeBufferParam(shader, kernel, WriteID, data.m_temp3f);
        cmd.SetComputeBufferParam(shader, kernel, VelocityID, data.m_velocity[READ]);

        cmd.DispatchCompute(shader,
            kernel,
            Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));
    }

    // m_computeConfinement
    static public void ComputeConfinement(CommandBuffer cmd, ComputeShader shader, WindDataRuntime data, float dt)
    {
        int kernel = 0;
        cmd.SetComputeVectorParam(shader, SizeID, data.size);
        cmd.SetComputeFloatParam(shader, DeltaTimeID, dt);
        cmd.SetComputeFloatParam(shader, EpsilonID, data.systemData.m_vorticityStrength);


        cmd.SetComputeBufferParam(shader, kernel, WriteID, data.m_velocity[WRITE]);
        cmd.SetComputeBufferParam(shader, kernel, ReadID, data.m_velocity[READ]);
        cmd.SetComputeBufferParam(shader, kernel, VorticityID, data.m_temp3f);

        cmd.DispatchCompute(shader,
            kernel,
            Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));

        Swap(data.m_velocity);
    }


    // m_computeDivergence
    static public void ComputeDivergence(CommandBuffer cmd, ComputeShader shader, WindDataRuntime data)
    {
        int kernel = 0;
        cmd.SetComputeVectorParam(shader, SizeID, data.size);

        cmd.SetComputeBufferParam(shader, kernel, WriteID, data.m_temp3f);
        cmd.SetComputeBufferParam(shader, kernel, VelocityID, data.m_velocity[READ]);
        cmd.SetComputeBufferParam(shader, kernel, ObstaclesID, data.m_obstacles);

        cmd.DispatchCompute(shader,
            kernel,
            Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));

    }


    // m_computeJacobi
    static public void ComputePressure(CommandBuffer cmd, ComputeShader shader, WindDataRuntime data)
    {

        int kernel = 0;
        cmd.SetComputeVectorParam(shader, SizeID, data.size);


        cmd.SetComputeBufferParam(shader, kernel, DivergenceID, data.m_temp3f);
        cmd.SetComputeBufferParam(shader, kernel, ObstaclesID, data.m_obstacles);

        for (int i = 0; i < data.systemData.m_iterations; i++)
        {
            cmd.SetComputeBufferParam(shader, kernel, WriteID, data.m_pressure[WRITE]);
            cmd.SetComputeBufferParam(shader, kernel, PressureID, data.m_pressure[READ]);


            cmd.DispatchCompute(shader,
                kernel,
                Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
                Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
                Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));

            Swap(data.m_pressure);
        }
    }


    static public void ComputeProjection(CommandBuffer cmd, ComputeShader shader, WindDataRuntime data)
    {
        int kernel = 0;
        cmd.SetComputeVectorParam(shader, SizeID, data.size);
        cmd.SetComputeBufferParam(shader, kernel, ObstaclesID, data.m_obstacles);

        cmd.SetComputeBufferParam(shader, kernel, PressureID, data.m_pressure[READ]);
        cmd.SetComputeBufferParam(shader, kernel, VelocityID, data.m_velocity[READ]);
        cmd.SetComputeBufferParam(shader, kernel, WriteID, data.m_velocity[WRITE]);

        cmd.DispatchCompute(shader,
            kernel,
            Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));

        Swap(data.m_velocity); 
    }
    static public void FinalizeWindData(CommandBuffer cmd, ComputeShader shader, WindDataRuntime data)
    {
        //shader.SetVector(WindAreaSizeID, data.basicData.WindAreaSize);
        //shader.SetVector(WindAreaSizeInvID, data.basicData.WindAreaSizeInv);
        //shader.SetFloat(WindResolutionID, data.basicData.WindResolution);
        //shader.SetInts(WindDataTexSizeID, new int[] { data.width, data.height, data.depth });
        //shader.SetTexture(kernel, WindDataTexID, data.WindDataTex[0]);

        
        int kernel = 0;
        cmd.SetComputeVectorParam(shader, SizeID, data.size);


        cmd.SetComputeBufferParam(shader, kernel, VelocityID, data.m_velocity[READ]);
        cmd.SetComputeBufferParam(shader, kernel, DensityID, data.m_density[READ]);
        cmd.SetComputeBufferParam(shader, kernel, ObstaclesID, data.m_obstacles);
        cmd.SetComputeBufferParam(shader, kernel, TemperatureID, data.m_temperature[READ]);
        cmd.SetComputeBufferParam(shader, kernel, PressureID, data.m_pressure[READ]);

        cmd.SetComputeTextureParam(shader, kernel, WindDataTexID, data.WindDataTex[WRITE]);

        cmd.DispatchCompute(shader,
            kernel,
            Mathf.CeilToInt(1.0f * data.width / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.height / NUM_THREADS),
            Mathf.CeilToInt(1.0f * data.depth / NUM_THREADS));

        Swap(data.WindDataTex);
    }

    // ========================================================================================

    // ================================ Output ================================================
    static public void ExportWindToVFX ( VisualEffect target , WindDataRuntime data )
    {
 
        target.SetVector3(WindAreaSizeInvID, data.basicData.WindAreaSizeInv);
        target.SetVector3(WindAreaSizeID, data.basicData.WindAreaSize);
        target.SetTexture(WindDataTexID, data.WindDataTex[READ]);
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
            Gizmos.DrawWireCube(pos , windDataLODs[i].basicData.WindAreaSize);
        }
    }
}
