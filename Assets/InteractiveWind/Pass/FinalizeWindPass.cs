using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

public class FinalizeWindPass : ScriptableRenderPass
{
    string m_ProfilerTag = "Wind_FinalizeWind";
    public ComputeShader finalizeShader;
    public int kernel;
    public static int WindDataTexID = Shader.PropertyToID("WindDataTex");

    public List<InteractiveWindManager.WindLODDataRT> windDataLODs;



    public FinalizeWindPass(WindFeature.WindSettings windSettings)
    {
        this.renderPassEvent = windSettings.renderEvent;
        this.finalizeShader = windSettings.finalizeShader;

        if ( finalizeShader != null )
            kernel = this.finalizeShader.FindKernel(windSettings.finalizeKernel);
    }

    public void Setup(List<InteractiveWindManager.WindLODDataRT> windDataLODs)
    {
        this.windDataLODs = windDataLODs;
    }


    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        ConfigureTarget(new RenderTargetIdentifier());
        ConfigureClear(ClearFlag.All, new Color(0, 0, 0, 0));
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera.tag.Equals("MainCamera"))
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            for (int i = 0; i < windDataLODs.Count; ++i)
            {
                var data = windDataLODs[i];
                InteractiveWindManager.AssignBasicMaterial(finalizeShader, data);
                cmd.SetComputeTextureParam(finalizeShader, kernel, WindDataTexID, data.WindDataTex[0]);
                cmd.DispatchCompute(finalizeShader, kernel,
                    Mathf.CeilToInt(1.0f * data.width  / WindFeature.THREAD_NUM),
                    Mathf.CeilToInt(1.0f * data.height / WindFeature.THREAD_NUM),
                    Mathf.CeilToInt(1.0f * data.depth  / WindFeature.THREAD_NUM));
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

    }
}
