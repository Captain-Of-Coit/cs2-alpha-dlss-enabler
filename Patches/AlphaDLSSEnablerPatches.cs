using Game.Rendering.Utilities;
using Game.Settings;
using Game.UI.Widgets;
using Game.UI.Menu;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static Game.Rendering.Utilities.AdaptiveDynamicResolutionScale;

namespace AlphaDLSSEnabler.Patches
{
    [HarmonyPatch(typeof(AdaptiveDynamicResolutionScale), "SetParams")]
    internal class AdaptiveDynamicResolutionScale_SetParamsPatch
    {
        static void Prefix(bool enabled, ref DynResUpscaleFilter filter, ref Camera camera)
        {
            if (camera != null)
            {
                MethodInfo GetFilterFromUiEnum = typeof(AdaptiveDynamicResolutionScale).GetMethod("GetFilterFromUiEnum", BindingFlags.NonPublic | BindingFlags.Static);
                DynamicResolutionHandler.SetUpscaleFilter(camera,
                    (!enabled)
                    ? DynamicResUpscaleFilter.CatmullRom
                    : (DynamicResUpscaleFilter)GetFilterFromUiEnum.Invoke(null, [filter])
                );
                camera.GetComponent<HDAdditionalCameraData>().allowDeepLearningSuperSampling = false;
                if (filter == DynResUpscaleFilter.DLSS)
                {
                    var hdcam = camera.GetComponent<HDAdditionalCameraData>();
                    hdcam.allowDeepLearningSuperSampling = true;
                    hdcam.allowDynamicResolution = true;
                    hdcam.deepLearningSuperSamplingQuality = 1;
                    hdcam.deepLearningSuperSamplingSharpening = 0.5f;
                    hdcam.deepLearningSuperSamplingUseCustomAttributes = true;
                    hdcam.deepLearningSuperSamplingUseOptimalSettings = true;
                    DynamicResolutionHandler.ClearSelectedCamera();
                }

            }
            return;
        }
    }

    [HarmonyPatch(typeof(AutomaticSettings.EnumDropdownValues), "GetValues",new Type[] {typeof(Type),typeof(string)})]
    internal class SettingVisibility
    {
        static List<EnumMember> Postfix(List<EnumMember> __result,Type underlyingType, string baseId)
        {
            string[] names = Enum.GetNames(underlyingType);
            Array values = Enum.GetValues(underlyingType);
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].Equals("DLSS"))
                {
                    int num = (int)values.GetValue(i);
                    __result.Add(new EnumMember((ulong)num, baseId + "." + underlyingType.Name.ToUpperInvariant() + "[" + names[i] + "]", !IsSelectable(num)));
                }
            }

            bool IsSelectable(int value)
            {
                if (underlyingType == typeof(QualitySetting.Level) && value == 6)
                {
                    return false;
                }
                return true;
            }
            return __result;
        }
    }
    [HarmonyPatch(typeof(AdaptiveDynamicResolutionScale), "GetFilterFromUiEnum")]
    internal class SettingsPatch
    {
        private static readonly byte DLSS = 6;
        
        static bool Prefix(ref DynResUpscaleFilter filter,ref DynamicResUpscaleFilter __result)
        {
            __result = filter switch
            {
                DynResUpscaleFilter.CatmullRom => DynamicResUpscaleFilter.CatmullRom,
                DynResUpscaleFilter.EdgeAdaptiveScaling => DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres,
                DynResUpscaleFilter.ContrastAdaptiveSharpen => DynamicResUpscaleFilter.TAAU,
                DynResUpscaleFilter.TAAU => DynamicResUpscaleFilter.ContrastAdaptiveSharpen,
                DynResUpscaleFilter.DLSS => (DynamicResUpscaleFilter)DLSS,
                _ => throw new NotSupportedException($"{filter} is not a supported upscaler"),
            };
            return false;
        }
    }
}