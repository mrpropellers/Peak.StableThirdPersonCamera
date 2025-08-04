using HarmonyLib;
using UnityEngine;

namespace Linkoid.Peak.StableCamera;

// [HarmonyPatch(typeof(Camera))]
// public static class OverrideCameraMain
// {
//     [HarmonyPatch("main"), HarmonyPatch(MethodType.Getter), HarmonyPrefix]
//     public static bool GetMain_Prefix(ref Camera __result)
//     {
//         if (StableCamera.Config.Enabled.Value && Cinemachine.BrainCamera != null)
//         {
//             __result = Cinemachine.BrainCamera;
//             return false;
//         }
//         
//         return true;
//     }
// }
// 
// [HarmonyPatch(typeof(MainCamera))]
// public static class OverrideMainCamera
// {
//     [HarmonyPatch("transform"), HarmonyPatch(MethodType.Getter), HarmonyPrefix]
//     public static bool GetTransform_Prefix(ref Transform __result)
//     {
//         if (StableCamera.Enabled && CinemachineTakeover.BrainCamera != null)
//         {
//             __result = CinemachineTakeover.BrainCamera.transform;
//             return false;
//         }
// 
//         return true;
//     }
// }


