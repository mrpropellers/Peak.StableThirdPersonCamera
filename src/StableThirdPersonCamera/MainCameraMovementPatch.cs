using HarmonyLib;
using Unity.Cinemachine;
using UnityEngine;
using Zorro.Core;

namespace StableThirdPersonCamera;

[HarmonyPatch(typeof(Singleton<MainCameraMovement>))]
static class SingletonPatch
{
    // [HarmonyPrefix, HarmonyPatch("Awake")]
    // static bool Awake_Prefix(Singleton<MainCameraMovement> __instance)
    // {
        // if (__instance is not MainCameraMovement mainCameraMovement)
        // {
            // Debug.LogWarning("<><> HARMONY LOG <><> I guess this patches other classes too?");
            // return true;
        // }
        // if (!StableCamera.Config.Enabled.Value || MainCameraMovement.Instance == null)
        // {
            // StableCamera.LogToScreen("Allowing MainCameraMovement to do it's weird singleton stuff");
            // return true;
        // }
        // 
        // StableCamera.LogToScreen("Blocking MainCameraMovement from blowing up its whole GameObject");
        // Component.Destroy(__instance);
        // return false;
    // 
    
}

// [HarmonyPatch(typeof(Billboard), nameof(Billboard.LateUpdate))]
// static class BillboardPatch
// {
//     static bool Prefix(Billboard __instance)
//     {
//         if (!StableCamera.Enabled)
//             return true;
//         __instance.transform.rotation = Quaternion.LookRotation(-(CinemachineTakeover.BrainCamera.transform.position - __instance.transform.position));
//         return false;
//     }
// }



[HarmonyPatch(typeof(MainCameraMovement))]
internal static class MainCameraMovementPatch
{
    public static Transform SubstituteTransform;
    
    static void ConditionallyDisableMeshRenderers(MainCameraMovement mainCamera)
    {
        // Roughly estimated value for when we're far enough into the ragdoll cam that we should start seeing the player body
        const float ragdollCamThreshold = 0.35f;
        
        if (CameraHelpers.PlayerBodyHider == null)
        {
            // Fetch this component because it has direct references to all the renderers we care about
            CameraHelpers.PlayerBodyHider = Character.localCharacter.GetComponentInChildren<HideTheBody>();
            if (CameraHelpers.PlayerBodyHider == null)
                return;
        }
        
        // Turn body rendering off IFF we are not using the 3rd person cam AND we're not passed out (putting us in 3rd person camera)
        var characterData = Character.localCharacter?.data;
        bool probablyInThirdPerson = StableThirdPersonCamera.Enabled;
        if (characterData != null)
        {
            var probablyInRagdollCam = mainCamera.ragdollCam > ragdollCamThreshold;
            probablyInThirdPerson = probablyInThirdPerson || probablyInRagdollCam ||
                characterData.passedOut || characterData.fullyPassedOut || characterData.dead;
        }

        // We are good to hide these meshes any time we're not in third person, even though they usually are not
        // hidden without the mod installed.
        // (This might be why the scout book disappears? If it attaches to the head mesh when open...)
        bool shouldShowPotentiallyClippingMeshes = probablyInThirdPerson;
        CameraHelpers.PlayerBodyHider.headRend.enabled = shouldShowPotentiallyClippingMeshes;
        CameraHelpers.PlayerBodyHider.sash.enabled = shouldShowPotentiallyClippingMeshes;
        foreach (var hatRenderer in CameraHelpers.PlayerBodyHider.refs.playerHats)
        {
            hatRenderer.enabled = shouldShowPotentiallyClippingMeshes;
        }

        // These meshes SHOULD show up in first-person view
        bool shouldShowNonClippingMeshes = probablyInThirdPerson || !StableThirdPersonCamera.Enabled;
        CameraHelpers.PlayerBodyHider.body.enabled = shouldShowNonClippingMeshes;
        foreach (var meshRenderer in CameraHelpers.PlayerBodyHider.costumes)
        {
            meshRenderer.enabled = shouldShowNonClippingMeshes;
        }
    }

    [HarmonyPrefix, HarmonyPatch(nameof(MainCameraMovement.CharacterCam))]
    static bool CharacterCam_Prefix(MainCameraMovement __instance)
    {
        if (CameraHelpers.ShouldSetUpCameras)
        {
            CameraHelpers.SetUpComponents(__instance.gameObject);
            __instance.cam.cam = CameraHelpers.DummyCamera;
        }
        // If we shouldn't set them up, and we also HAVEN'T set them up, let the game code run like normal
        else if (!CameraHelpers.HasSetUpCameras)
        {
            return true;
        }
        // Call this before the early return to ensure the meshes get re-enabled if StableCamera is disabled in-game
        //ConditionallyDisableMeshRenderers(__instance);

        ConditionallyDisableMeshRenderers(__instance);
        // CameraHelpers.FollowCamera.enabled = camsEnabled;
        // CameraHelpers.ClimbCamera.enabled = camsEnabled;
        //CameraHelpers.FirstPersonCamera.enabled = !StableThirdPersonCamera.Enabled;
        //Cinemachine.BrainCamera.gameObject.SetActive(camsEnabled);
        //MainCamera.instance.cam.enabled = !camsEnabled;
        // if (!StableThirdPersonCamera.Config.Enabled.Value)
        // {
        //     
        //     // FollowCamera.Priority = int.MinValue;
        //     // ClimbCamera.Priority = int.MinValue;
        //     return true;
        // }

        if (Character.localCharacter == null) return false;

        var characterState = Character.localCharacter.data;
        var playerIsClimbing = characterState != null 
            && (characterState.isClimbing || characterState.isRopeClimbing || characterState.isVineClimbing);
        CameraHelpers.UpdateVCamPriorities(playerIsClimbing);
        //     && !(characterState.isRopeClimbing || characterState.isVineClimbing);
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
            // Changed
            // if (!StableCamera.Config.ThirdPersonRagdoll.Value)
            // {
            //     SubstituteTransform.rotation = Quaternion.Lerp(
            //         SubstituteTransform.rotation,
            //         __instance.physicsRot,
            //         __instance.ragdollCam * StableCamera.Config.DizzyEffectStrength.Value // Changed
            //     );
            // }

            // Changed
            // var shakeRotation = Quaternion.Euler(GamefeelHandler.instance.GetRotation());
            // SubstituteTransform.Rotate(Quaternion.identity.eulerAngles, Space.World); 
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

    [HarmonyPrefix, HarmonyPatch(nameof(MainCameraMovement.GetFov))]
    static bool GetFov_Prefix(MainCameraMovement __instance, ref float __result)
    {
        if (!CameraHelpers.HasSetUpCameras) return true;
        if (Character.localCharacter == null) return true;

        float num = __instance.fovSetting.Value;
        if (num < 60f)
        {
            num = 70f;
        }
        
        __instance.currentFov = Mathf.Lerp(
            __instance.currentFov,
            num + (Character.localCharacter.data.isClimbing ? StableThirdPersonCamera.Config.ExtraClimbingFOV.Value : 0), // Changed
            Time.deltaTime * 5f
        );

        __result = __instance.currentFov;
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.GetCameraPos))]
    static bool GetCameraPos_Prefix(Character __instance, ref Vector3 __result, float forwardOffset)
    {
        if (!CameraHelpers.HasSetUpCameras) return true;

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