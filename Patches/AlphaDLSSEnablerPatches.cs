using HarmonyLib;
using UnityEngine;
using Game.Rendering.Utilities;
using UnityEngine.Rendering.HighDefinition;

namespace AlphaDLSSEnabler.Patches {
    [HarmonyPatch(typeof(AdaptiveDynamicResolutionScale), "SetParams")]
    internal class AdaptiveDynamicResolutionScale_SetParamsPatch {
       static void Prefix(ref AdaptiveDynamicResolutionScale.DynResUpscaleFilter filter, Camera camera) {
           filter = AdaptiveDynamicResolutionScale.DynResUpscaleFilter.DLSS;
            var hdcam = camera.GetComponent<HDAdditionalCameraData>();
            hdcam.allowDeepLearningSuperSampling = true;
            hdcam.allowDynamicResolution = true;
            hdcam.deepLearningSuperSamplingQuality = 1;
            hdcam.deepLearningSuperSamplingSharpening = 0.5f;
            hdcam.deepLearningSuperSamplingUseCustomAttributes = true;
            hdcam.deepLearningSuperSamplingUseOptimalSettings = true;
            UnityEngine.Debug.Log("DL Super Sampling enabled");
        }
    }
}