using HarmonyLib;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using Zorro.Core;
using Unity.Mathematics;

namespace Linkoid.Peak.StableCamera;

[HarmonyPatch(typeof(Zorro.Core.DistanceDisablerHandler))]
public static class DistanceDisablerFix
{
    public class FakeCamera
    {
        Camera main => MainCamera.instance.cam ?? Camera.main;
    }

    [HarmonyPrefix, HarmonyPatch("Update")]
    static bool UpdateFix(DistanceDisablerHandler __instance)
    {
        if (UnityEngine.Camera.main != null || MainCamera.instance.cam == null)
            return true;
        var accessor = Traverse.Create(__instance);
        float3 position = (float3) MainCamera.instance.cam.transform.position;
        accessor.Field("m_eventQueue").SetValue(new NativeQueue<DistanceDisablerEvent>((AllocatorManager.AllocatorHandle) Allocator.TempJob));
        accessor.Field("m_jobHandle").SetValue(Optionable<JobHandle>.Some(new DistanceDisablerJob()
        {
            DistanceDisablerData = accessor.Field("m_distanceDisablerDataRecord").GetValue<NativeListRecord<DistanceDisablerData>>().NativeList.AsArray(),
            CameraPosition = position,
            DistanceDisablerEventQueue = accessor.Field("m_eventQueue").GetValue<NativeQueue<DistanceDisablerEvent>>().AsParallelWriter()
        }.Schedule<DistanceDisablerJob>(accessor.Field("m_transformAccessRecord").GetValue<TransformAccessRecord>().TransformAccessArray)));
        return false;
    }
}
