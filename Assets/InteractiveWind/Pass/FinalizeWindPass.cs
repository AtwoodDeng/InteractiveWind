using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

public class FinalizeWindPass : ScriptableRenderPass 
{
    string m_ProfilerTag = "Wind_Finalize";
    //public ComputeShader finalizeShader;
    //public int kernel;
    public static int WindDataTexID = Shader.PropertyToID("WindDataTex"); 

    public List<InteractiveWindManager.WindDataRuntime> windDatas;
    public WindFeature.WindSettings settings;

    public FinalizeWindPass(WindFeature.WindSettings windSettings)
    {
        this.renderPassEvent = windSettings.renderEvent;
        this.settings = windSettings;

        //if ( finalizeShader != null )
        //    kernel = this.finalizeShader.FindKernel(windSettings.finalizeKernel);
    }

    public void Setup(List<InteractiveWindManager.WindDataRuntime> windDataLODs)
    {
        this.windDatas = windDataLODs; 
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

            for (int i = 0; i < windDatas.Count; ++i)
            {
                var data = windDatas[i];
                InteractiveWindManager.FinalizeWindData(
                    cmd,
                    settings.finalizeShader, 
                    data
                );
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

    }
}
