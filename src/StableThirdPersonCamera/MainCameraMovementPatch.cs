using HarmonyLib;
using UnityEngine;

namespace StableThirdPersonCamera;

[HarmonyPatch(typeof(MainCameraMovement))]
internal static class MainCameraMovementPatch
{
    public static Transform SubstituteTransform;

    [HarmonyPrefix, HarmonyPatch(nameof(MainCameraMovement.CharacterCam))]
    static bool CharacterCam_Prefix(MainCameraMovement __instance)
    {
        if (Cameras.ShouldSetUpCameras)
        {
            Cameras.SetUpComponents(__instance.gameObject);
            __instance.cam.cam = Cameras.DummyCamera;
        }
        // If we shouldn't set them up, and we also HAVEN'T set them up, let the game code run like normal
        else if (!Cameras.HasSetUpCameras)
        {
            return true;
        }

        if (Character.localCharacter == null) return false;

        __instance.cam.cam.fieldOfView = __instance.GetFov();
        
        // Handle Rotation
        if (Character.localCharacter.data.lookDirection != Vector3.zero)
        {
            SubstituteTransform.rotation = Quaternion.LookRotation(Character.localCharacter.data.lookDirection);
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
        }

        Vector3 cameraPos = Character.localCharacter.GetCameraPos(__instance.GetHeadOffset());
        Vector3 torsoPos = GetRagdollCameraPosition(__instance); // Changed

        __instance.targetPlayerPovPosition = Vector3.Lerp(cameraPos, torsoPos, __instance.ragdollCam);
        var distance = Vector3.Distance(SubstituteTransform.position, __instance.targetPlayerPovPosition);
        if (distance > __instance.characterPovMaxDistance)
        {
            // Changed
            var clampedTargetPlayerPovPosition = __instance.targetPlayerPovPosition + (SubstituteTransform.position - __instance.targetPlayerPovPosition).normalized * __instance.characterPovMaxDistance;
            SubstituteTransform.position = Vector3.Lerp(SubstituteTransform.position, clampedTargetPlayerPovPosition, Time.deltaTime * (__instance.characterPovLerpRate + distance / __instance.characterPovMaxDistance * StableThirdPersonCamera.Config.TrackingPower.Value));
            
        }

        SubstituteTransform.position = Vector3.Lerp(SubstituteTransform.position, __instance.targetPlayerPovPosition, Time.deltaTime * __instance.characterPovLerpRate);

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.GetCameraPos))]
    static bool GetCameraPos_Prefix(Character __instance, ref Vector3 __result, float forwardOffset)
    {
        if (!Cameras.HasSetUpCameras) return true;

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
    
    static Vector3 CenterOfMass(this CharacterRagdoll ragdoll)
    {
        Vector3 positionSum = Vector3.zero;
        float massSum = 0.0f;
        foreach (var bodypart in ragdoll.partList)
        {
            var rig = bodypart.Rig;
            positionSum += bodypart.transform.TransformPoint(rig.centerOfMass) * rig.mass;
            massSum += rig.mass;
        }

        return positionSum / massSum;
    }

    static Vector3 GetRagdollCameraPosition(MainCameraMovement __instance)
    {
        //Transform torso = Character.localCharacter.GetBodypart(BodypartType.Torso).transform;

        // if (!StableCamera.Config.ThirdPersonRagdoll.Value)
        //     return torso.position;

        Vector3 lookDirection = Character.localCharacter.data.lookDirection;
        if (lookDirection == Vector3.zero)
            lookDirection = SubstituteTransform.forward;

        lookDirection = lookDirection.normalized;

        float distance = 3f * __instance.ragdollCam * __instance.ragdollCam;

        Vector3 anchorPos = GetCameraAnchor(Character.localCharacter, __instance.GetHeadOffset());
        Vector3 desiredPos = anchorPos - lookDirection * distance;
        //if (Physics.SphereCast(anchorPos, 0.06f, -lookDirection, out RaycastHit hit, distance, LayerMask.GetMask("Terrain", "Map")))
        //    desiredPos = hit.point + lookDirection * 0.03f;

        return desiredPos;
    }

}