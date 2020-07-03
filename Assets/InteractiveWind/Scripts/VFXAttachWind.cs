using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class VFXAttachWind : MonoBehaviour
{
    public void OnEnable()
    {
        InteractiveWindManager.Register(GetComponent<VisualEffect>());
    }

    public void OnDisable()
    {
        InteractiveWindManager.Unregister(GetComponent<VisualEffect>());
    }
}
