using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace AlbaVR
{
    public class VRQualitySettings : MonoBehaviour
    {
        private Camera _vrCamera;
        private PostProcessLayer _vrPostProcessLayer;

        private void Start()
        {
            _vrCamera = GetComponent<Camera>();
            
            PostProcessResources postProcessResources = Resources.FindObjectsOfTypeAll<PostProcessResources>()[0];
            _vrPostProcessLayer = _vrCamera.gameObject.AddComponent<PostProcessLayer>();
            _vrPostProcessLayer.Init(postProcessResources);

            PerformanceConfigPatcher.PerformanceConfigApplied += ApplyPerformanceConfig;

            ApplyPerformanceConfig(GraphicsSettingsPrefs.deviceToPerformanceConfig.currentConfig);
        }

        private void ApplyPerformanceConfig(PerformanceConfig performanceConfig)
        {
            UpdateAntiAliasing(performanceConfig.antiAliasingLevel);
            UpdateLodBias(performanceConfig.LODConfig.lodBias);
        }

        private void UpdateAntiAliasing(PerformanceConfig.AALevel antiAliasingLevel)
        {
            if (antiAliasingLevel == PerformanceConfig.AALevel._smaa)
            {
                _vrPostProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
                _vrPostProcessLayer.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.Medium;

                // The game also enables 4x MSAA when SMAA is enabled. Don't know if this is deliberate but it doesn't
                // make much sense to me, especially in VR. So let's undo it.
                QualitySettings.antiAliasing = 0;
            }
            else
            {
                _vrPostProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.None;
            }
        }

        private void UpdateLodBias(float originalLodBias)
        {
            float lodBiasMultiplier = Settings.LodBiasMultiplier.Value;
            if (lodBiasMultiplier <= 0f)
            {
                // The higher FOV in VR means that LODs appear to transition/cull faster than in non-VR. We can use the
                // following calculation to find a LOD bias multiplier that's consistent with the LOD/cull distances of
                // the non-VR third-person camera's 50 degree FOV. See also:
                // https://forum.unity.com/threads/lodgroup-in-vr.455394/#post-2952522

                float referenceFieldOfView = 50f * Mathf.Deg2Rad;
                float vrFieldOfView = _vrCamera.fieldOfView * Mathf.Deg2Rad;
                lodBiasMultiplier = Mathf.Tan(vrFieldOfView / 2f) / Mathf.Tan(referenceFieldOfView / 2f);
            }

            QualitySettings.lodBias = originalLodBias * lodBiasMultiplier;
        }
    }
}
