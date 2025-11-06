using System;
using UnityEngine;

namespace StableThirdPersonCamera;

public class CameraPrioritySetter : MonoBehaviour
{
    const float k_OffClimbTransitionDelay = 0.75f;
    
    public static int TopPriority = 9999;
    public static int FirstPersonDefaultPriority = 1;
    public static int ClimbDefaultPriority = 2;
    public static int FollowDefaultPriority = 3;

    float _timeLastClimbed;

    void Update()
    {
        if (!Cameras.HasSetUpCameras)
            return;

        if (!StableThirdPersonCamera.Enabled)
        {
            Cameras.FirstPersonCamera.Priority = TopPriority + 1;
            return;
        }

        Cameras.FirstPersonCamera.Priority = FirstPersonDefaultPriority;
        
        var characterState = Character.localCharacter.data;
        var playerIsClimbing = characterState != null && characterState.isClimbingAnything;
        if (playerIsClimbing)
        {
            _timeLastClimbed = Time.time;
        }
        if (Cameras.IsStartingThrowCharge)
        {
            Cameras.AimCamera.Priority = TopPriority + 1;
        }
        else
        {
            var inClimbCam = Time.time - _timeLastClimbed < k_OffClimbTransitionDelay;
            Cameras.AimCamera.Priority = FirstPersonDefaultPriority;
            Cameras.FollowCamera.Priority = inClimbCam ? FollowDefaultPriority : TopPriority;
            Cameras.ClimbCamera.Priority = inClimbCam ? TopPriority : FollowDefaultPriority;
        }
    }
}