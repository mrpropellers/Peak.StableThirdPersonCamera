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

        if (Character.localCharacter == null)
            // The orginal method returns here, but let it run anyway for better compatability with other mods.
            return true;

        __instance.cam.cam.fieldOfView = __instance.GetFov();
        if (Character.localCharacter == null || Character.localCharacter == null) return true; // Same as above.

        // Handle Rotation
        if (Character.localCharacter.data.lookDirection != Vector3.zero)
        {
            __instance.transform.rotation = Quaternion.LookRotation(Character.localCharacter.data.lookDirection);
            float ragdollCamControl = 1f - Character.localCharacter.data.GetCurrentRagdollControll();

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

            // Changed `base.transform.rotation = Quaternion.Lerp(base.transform.rotation, physicsRot, ragdollCam);`
            if (!StableCamera.Config.ThirdPersonRagdoll.Value)
            {
                __instance.transform.rotation = Quaternion.Lerp(
                    __instance.transform.rotation,
                    __instance.physicsRot,
                    __instance.ragdollCam * StableCamera.Config.DizzyEffectStrength.Value // Changed
                );
            }

            // Changed `base.transform.Rotate(GamefeelHandler.instance.GetRotation(), Space.World);`
            var shakeRotation = Quaternion.Euler(GamefeelHandler.instance.GetRotation());
            __instance.transform.Rotate(Quaternion.Slerp(Quaternion.identity, shakeRotation, StableCamera.Config.ShakeEffectStrength.Value).eulerAngles, Space.World); 
        }

        Vector3 cameraPos = Character.localCharacter.GetCameraPos(__instance.GetHeadOffset());
        Vector3 torsoPos = GetRagdollCameraPosition(__instance); // Changed `Character.localCharacter.GetBodypart(BodypartType.Torso).transform.position;`

        __instance.targetPlayerPovPosition = Vector3.Lerp(cameraPos, torsoPos, __instance.ragdollCam);
        var distance = Vector3.Distance(__instance.transform.position, __instance.targetPlayerPovPosition);
        if (distance > __instance.characterPovMaxDistance)
        {
            // Changed `base.transform.position = targetPlayerPovPosition + (base.transform.position - targetPlayerPovPosition).normalized * characterPovMaxDistance;`
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

    [HarmonyPrefix, HarmonyPatch(nameof(MainCameraMovement.GetFov))]
    static bool GetFov_Prefix(MainCameraMovement __instance, ref float __result)
    {
        if (!StableCamera.Config.Enabled.Value) return true;
        if (Character.localCharacter == null) return true;

        float num = __instance.fovSetting.Value;
        if (num < 60f)
        {
            num = 70f;
        }
        
        __instance.currentFov = Mathf.Lerp(
            __instance.currentFov,
            num + (Character.localCharacter.data.isClimbing ? StableCamera.Config.ExtraClimbingFOV.Value : 0), // Changed
            Time.deltaTime * 5f
        );

        __result = __instance.currentFov;
        return false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.GetCameraPos))]
    static void GetCameraPos_Postfix(Character __instance, ref Vector3 __result, float forwardOffset)
    {
        if (!StableCamera.Config.Enabled.Value) return;
        if (!StableCamera.Config.StabilizeTracking.Value) return;

        var stablePos = GetCameraAnchor(__instance, forwardOffset);

        // Prevent camera from clipping through cliffs
        var radius = MainCamera.instance.cam.nearClipPlane * 1.7320508075688772f; // √3
        var offset = stablePos - __result;
        var distance = offset.magnitude;
        var direction = offset / distance;
        if (Physics.SphereCast(__result, radius, direction, out RaycastHit hit, distance, LayerMask.GetMask("Terrain", "Map")))
        {
            __result += direction * hit.distance;
        }
        else
        {
            __result = stablePos;
        }
    }



    static Vector3 GetCameraAnchor(Character character, float forwardOffset)
    {
        var head = character.GetBodypart(BodypartType.Head);
        //return head.transform.TransformPoint(Vector3.up * 1f + Vector3.forward * forwardOffset);

        if (!head.TryGetComponent(out BodypartStableData headStableData))
        {
            headStableData = head.gameObject.AddComponent<BodypartStableData>();
        }

        var upVector   = Vector3.Scale(head.transform.lossyScale, headStableData.InterpolatedTargetUp());
        var lookVector = Vector3.Scale(head.transform.lossyScale, headStableData.InterpolatedTargetForward());
        var transformedOffset = upVector + lookVector * forwardOffset;

        var headTargetOffset = headStableData.InterpolatedTargetOffsetRelativeToCenterOfMass();

        return character.refs.ragdoll.WorldCenterOfMass() + headTargetOffset + transformedOffset;
    }

    // This method is an ideal way to determine a smooth and stable anchor point, 
    // but it alters the position of the body every frame which breaks interpolation,
    // and causes items to jitter.
    static Vector3 GetCameraAnchor_EXPERIMENTAL(Character character, float forwardOffset)
    {
        var head = character.GetBodypart(BodypartType.Head);
        var hip = character.GetBodypart(BodypartType.Hip);

        // Set character to pure animation pose (no IK)
        character.refs.animator.playableGraph.Evaluate();

        var expectedCenterOfMass = character.refs.ragdoll.WorldCenterOfMass();
        var expectedPosition = head.transform.TransformPoint(Vector3.up * 1f + Vector3.forward * forwardOffset);
        var expectedOffset = expectedPosition - expectedCenterOfMass;

        // Revert character back to original position (determined by IK)
        foreach (var part in character.refs.ragdoll.partList)
        {
            part.ResetTransform();
        }

        var actualCenterOfMass = character.refs.ragdoll.WorldCenterOfMass();

        return actualCenterOfMass + expectedOffset;
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
