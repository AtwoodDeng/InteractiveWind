using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindFeature : UnityEngine.Rendering.Universal.ScriptableRendererFeature
{

    [System.Serializable]
    public class WindSettings
    {
        public UnityEngine.Rendering.Universal.RenderPassEvent renderEvent = UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingOpaques;
        public ComputeShader finalizeShader = null;
        public string finalizeKernel = "CSMain";
    }

    public WindSettings settings;
    static public int THREAD_NUM = 8;

    FinalizeWindPass finalizePass;

    public override void Create()
    {
        finalizePass = new FinalizeWindPass(settings);
    }

    public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
    {
        if (InteractiveWindManager.Instance == null || !InteractiveWindManager.Instance.IsReady() )
            return;

        if (settings.finalizeShader != null )
        {
            finalizePass.Setup(InteractiveWindManager.Instance.windDataLODs);
            renderer.EnqueuePass(finalizePass);
        }
    }
}
