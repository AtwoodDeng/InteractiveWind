using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ComputeWindPass : ScriptableRenderPass
{
    string m_ProfilerTag = "Wind_Compute";
    WindFeature.WindSettings windSettings;
    //You can change this or even use Time.DeltaTime but large time steps can cause numerical errors
    const float TIME_STEP = 0.1f;

    public List<InteractiveWindManager.WindDataRuntime> windDatas;


    public ComputeWindPass(WindFeature.WindSettings windSettings)
    {
        this.renderPassEvent = windSettings.renderEvent;

        this.windSettings = windSettings;

    }
    public void Setup(List<InteractiveWindManager.WindDataRuntime> windDatas)
    {
        this.windDatas = windDatas;
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

            float dt = TIME_STEP;

            for (int i = 0; i < windDatas.Count; ++i)
            {
                var data = windDatas[i];

                InteractiveWindManager.ComputeObstacles(
                    cmd,
                    windSettings.m_computeObstacles,
                    data);


                InteractiveWindManager.ApplyAdvection(
                    cmd,
                    windSettings.m_applyAdvect,
                    data,
                    dt,
                    data.systemData.m_temperatureDissipation,
                    0.0f,
                    data.m_temperature);

                if (data.systemData.m_advectionType == WindSystemData.ADVECTION.BFECC)
                {
                    // do nothing
                }
                else if (data.systemData.m_advectionType == WindSystemData.ADVECTION.BFECC)
                {

                }
                else
                {
                    InteractiveWindManager.ApplyAdvection(
                        cmd,
                        windSettings.m_applyAdvect,
                        data,
                        dt,
                        data.systemData.m_densityDissipation,
                        0.0f,
                        data.m_density);
                }

                InteractiveWindManager.ApplyAdvectionVelocity(
                        cmd,
                        windSettings.m_applyAdvect,
                        data,
                        dt
                    );


                InteractiveWindManager.ApplyBuoyancy(
                        cmd,
                        windSettings.m_applyBuoyancy,
                        data,
                        dt
                    );

                InteractiveWindManager.ApplyImpulse(
                        cmd,
                        windSettings.m_applyImpulse,
                        data,
                        dt,
                        data.systemData.m_densityAmount,
                        data.m_density
                    );

                InteractiveWindManager.ApplyImpulse(
                        cmd,
                        windSettings.m_applyImpulse,
                        data,
                        dt,
                        data.systemData.m_temperatureAmount,
                        data.m_temperature
                    );

                InteractiveWindManager.ComputeVorticity(
                        cmd,
                        windSettings.m_computeVorticity,
                        data,
                        dt
                    );

                InteractiveWindManager.ComputeConfinement(
                        cmd,
                        windSettings.m_computeConfinement,
                        data,
                        dt
                    );

                InteractiveWindManager.ComputeDivergence(
                        cmd,
                        windSettings.m_computeDivergence,
                        data
                    );

                InteractiveWindManager.ComputePressure(
                        cmd,
                        windSettings.m_computeJacobi,
                        data
                    );

                InteractiveWindManager.ComputeProjection(
                        cmd,
                        windSettings.m_computeProjection,
                        data
                    );
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

    }
}
