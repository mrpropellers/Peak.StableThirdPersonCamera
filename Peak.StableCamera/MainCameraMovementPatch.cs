using HarmonyLib;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Linkoid.Peak.StableCamera;

[HarmonyPatch(typeof(MainCameraMovement))]
internal static class MainCameraMovementPatch
{
    [HarmonyPrefix, HarmonyPatch(nameof(MainCameraMovement.CharacterCam))]
    static bool CharacterCam_Prefix(MainCameraMovement __instance)
    {
        if (!StableCamera.Config.Enabled.Value) return true;

        if (Character.localCharacter == null) return false;

        __instance.cam.cam.fieldOfView = __instance.GetFov();
        if (Character.localCharacter == null || Character.localCharacter == null) return false;

        // Handle Rotation
        if (Character.localCharacter.data.lookDirection != Vector3.zero)
        {
            __instance.transform.rotation = Quaternion.LookRotation(Character.localCharacter.data.lookDirection);
            float ragdollCamControl = 1f - Character.localCharacter.data.currentRagdollControll;

            // Update ragdollCam value
            if (ragdollCamControl > __instance.ragdollCam)
            {
                __instance.ragdollCam = Mathf.Lerp(__instance.ragdollCam, ragdollCamControl, Time.deltaTime * 5f);
            }
            else
            {
                __instance.ragdollCam = Mathf.Lerp(__instance.ragdollCam, ragdollCamControl, Time.deltaTime * 0.5f);
            }

            // Apply ragdollCam rotation
            __instance.physicsRot = Quaternion.Lerp(__instance.physicsRot, Character.localCharacter.GetBodypartRig(BodypartType.Head).transform.rotation, Time.deltaTime * 10f);
            __instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, __instance.physicsRot, __instance.ragdollCam * StableCamera.Config.DizzyEffectStrength.Value); // Changed
            __instance.transform.Rotate(GamefeelHandler.instance.GetRotation(), Space.World);
        }

        Vector3 cameraPos = Character.localCharacter.GetCameraPos(__instance.GetHeadOffset());
        Vector3 torsoPos = GetRagdollCameraPosition(__instance); // Changed

        __instance.targetPlayerPovPosition = Vector3.Lerp(cameraPos, torsoPos, __instance.ragdollCam);
        var distance = Vector3.Distance(__instance.transform.position, __instance.targetPlayerPovPosition);
        if (distance > __instance.characterPovMaxDistance)
        {
            // Changed
            var clampedTargetPlayerPovPosition = __instance.targetPlayerPovPosition + (__instance.transform.position - __instance.targetPlayerPovPosition).normalized * __instance.characterPovMaxDistance;
            if (StableCamera.Config.StabilizeTracking.Value)
            {
                __instance.transform.position = Vector3.Lerp(__instance.transform.position, clampedTargetPlayerPovPosition, Time.deltaTime * (__instance.characterPovLerpRate + distance / __instance.characterPovMaxDistance * StableCamera.Config.TrackingPower.Value));
            }
            else
            {
                __instance.transform.position = clampedTargetPlayerPovPosition;
            }
            
        }

        __instance.transform.position = Vector3.Lerp(__instance.transform.position, __instance.targetPlayerPovPosition, Time.deltaTime * __instance.characterPovLerpRate);

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.GetCameraPos))]
    static bool GetCameraPos_Prefix(Character __instance, ref Vector3 __result, float forwardOffset)
    {
        if (!StableCamera.Config.StabilizeTracking.Value) return true;

        __result = GetCameraAnchor(__instance, forwardOffset);

        return false;
    }

    static Vector3 GetCameraAnchor(Character character, float forwardOffset)
    {
        var head = character.GetBodypart(BodypartType.Head);
        //return head.transform.TransformPoint(Vector3.up * 1f + Vector3.forward * forwardOffset);

        Vector3 lookDirection = character.data.lookDirection;
        if (lookDirection == Vector3.zero)
            lookDirection = character.refs.head.transform.forward;

        var upVector   = Vector3.Scale(character.refs.head.transform.lossyScale, Vector3.up);
        var lookVector = Vector3.Scale(character.refs.head.transform.lossyScale, lookDirection);
        //var transformedOffset = character.refs.head.transform.TransformVector(offset);
        var transformedOffset = upVector + lookVector * forwardOffset;

        //return head.WorldTargetPos() + transformedOffset;

        return character.refs.ragdoll.CenterOfMass() + transformedOffset + head.targetOffsetRelativeToHip * 0.75f;
    }

    static Vector3 GetRagdollCameraPosition(MainCameraMovement __instance)
    {
        Transform torso = Character.localCharacter.GetBodypart(BodypartType.Torso).transform;

        if (!StableCamera.Config.ThirdPersonRagdoll.Value)
            return torso.position;

        Vector3 lookDirection = Character.localCharacter.data.lookDirection;
        if (lookDirection == Vector3.zero)
            lookDirection = __instance.transform.forward;

        lookDirection = lookDirection.normalized;

        float distance = 3f * __instance.ragdollCam * __instance.ragdollCam;

        Vector3 anchorPos = GetCameraAnchor(Character.localCharacter, __instance.GetHeadOffset());
        Vector3 desiredPos = anchorPos - lookDirection * distance;
        //if (Physics.SphereCast(anchorPos, 0.06f, -lookDirection, out RaycastHit hit, distance, LayerMask.GetMask("Terrain", "Map")))
        //    desiredPos = hit.point + lookDirection * 0.03f;

        return desiredPos;
    }
}