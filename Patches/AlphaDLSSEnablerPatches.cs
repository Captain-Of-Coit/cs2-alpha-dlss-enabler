/*
MIT License

Copyright (c) 2023-2024 Captain-Of-Coit, repletsin5

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
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

    [HarmonyPatch(typeof(AutomaticSettings), "GetEnumValues", new Type[] {typeof(Type),typeof(string)})]
    internal class SettingVisibility
    {
        static EnumMember[] Postfix(EnumMember[] __result,Type underlyingType, string prefix)
        {
            string[] names = Enum.GetNames(underlyingType);
            Array values = Enum.GetValues(underlyingType);
            List<EnumMember> temp = new List<EnumMember>(__result);
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].Equals("DLSS"))
                {
                    int num = (int)values.GetValue(i);
                    temp.Add(new EnumMember((ulong)num, prefix + "." + underlyingType.Name.ToUpperInvariant() + "[" + names[i] + "]", !IsSelectable(num)));
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
            __result = temp.ToArray();
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