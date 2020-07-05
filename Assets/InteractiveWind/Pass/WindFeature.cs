using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindFeature : UnityEngine.Rendering.Universal.ScriptableRendererFeature
{

    [System.Serializable]
    public class WindSettings
    {
        public UnityEngine.Rendering.Universal.RenderPassEvent renderEvent = UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingOpaques;
        public ComputeShader finalizeShader;
        //public string finalizeKernel = "CSMain";
        public ComputeShader m_applyImpulse, m_applyAdvect, m_computeVorticity;
        public ComputeShader m_computeDivergence, m_computeJacobi, m_computeProjection;
        public ComputeShader m_computeConfinement, m_computeObstacles, m_applyBuoyancy;

        public bool IsReady()
        {
            return finalizeShader != null
                && m_applyImpulse != null
                && m_applyAdvect != null
                && m_computeVorticity != null
                && m_computeDivergence != null
                && m_computeJacobi != null
                && m_computeProjection != null
                && m_computeConfinement != null
                && m_computeObstacles != null
                && m_applyBuoyancy != null; 

        }
    }

    public WindSettings settings;
    static public int THREAD_NUM = 8;

    FinalizeWindPass finalizePass;
    ComputeWindPass computeWindPass;

    public override void Create()
    {
        finalizePass = new FinalizeWindPass(settings);
        computeWindPass = new ComputeWindPass(settings);
    }

    public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
    {
        if (InteractiveWindManager.Instance == null || !InteractiveWindManager.Instance.IsReady() || !settings.IsReady())
            return;

        computeWindPass.Setup(InteractiveWindManager.Instance.windDataLODs);
        renderer.EnqueuePass(computeWindPass);
        finalizePass.Setup(InteractiveWindManager.Instance.windDataLODs);
        renderer.EnqueuePass(finalizePass);
    }
}
