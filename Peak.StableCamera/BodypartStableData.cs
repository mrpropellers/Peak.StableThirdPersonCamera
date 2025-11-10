using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Linkoid.Peak.StableCamera;

[HarmonyPatch(typeof(MainCameraMovement))]
public class BodypartStableData : MonoBehaviour
{
    [SerializeField] private Bodypart bodypart;

    [SerializeField] internal Vector3 targetOffsetRelativeToCenterOfMass;
    [SerializeField] private Vector3 previousTargetOffset;
    [SerializeField] private Vector3 previousTargetForward;
    [SerializeField] private Vector3 previousTargetUp;

    void Awake()
    {
        bodypart = this.GetComponent<Bodypart>();
        
        targetOffsetRelativeToCenterOfMass = bodypart.targetOffsetRelativeToHip * 0.75f;
        previousTargetOffset = targetOffsetRelativeToCenterOfMass;
        previousTargetForward = bodypart.targetForward;
        previousTargetUp = bodypart.targetUp;
    }

    public Vector3 InterpolatedTargetOffsetRelativeToCenterOfMass()
    {
        return Vector3.LerpUnclamped(
            previousTargetOffset,
            targetOffsetRelativeToCenterOfMass,
            (Time.time - Time.fixedTime) / Time.fixedDeltaTime
        );
    }

    public Vector3 InterpolatedTargetForward()
    {
        return Vector3.SlerpUnclamped(
            previousTargetForward,
            bodypart.targetForward,
            (Time.time - Time.fixedTime) / Time.fixedDeltaTime
        );
    }

    public Vector3 InterpolatedTargetUp()
    {
        return Vector3.SlerpUnclamped(
            previousTargetUp,
            bodypart.targetUp,
            (Time.time - Time.fixedTime) / Time.fixedDeltaTime
        );
    }

    public void SaveStableData()
    {
        previousTargetOffset = targetOffsetRelativeToCenterOfMass;
        previousTargetForward = bodypart.targetForward;
        previousTargetUp = bodypart.targetUp;

        targetOffsetRelativeToCenterOfMass = bodypart.WorldCenterOfMass() - bodypart.character.refs.ragdoll.WorldCenterOfMass();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Bodypart), nameof(Bodypart.SaveAnimationData))]
    static void SaveAnimationData_Prefix(Bodypart __instance)
    {
        if (__instance.TryGetComponent(out BodypartStableData stableData))
        {
            stableData.SaveStableData();
        }
    }
}
