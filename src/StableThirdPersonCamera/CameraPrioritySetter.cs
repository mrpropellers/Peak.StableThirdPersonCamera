using System;
using UnityEngine;

namespace StableThirdPersonCamera;

public class CameraPrioritySetter : MonoBehaviour
{
    public static int TopPriority = 9999;
    public static int FirstPersonDefaultPriority = 1;
    public static int ClimbDefaultPriority = 2;
    public static int FollowDefaultPriority = 3;

    bool _wasClimbing;
    
    void Update()
    {
        if (!Cameras.HasSetUpCameras)
            return;
        Cameras.FirstPersonCamera.Priority = StableThirdPersonCamera.Enabled 
            ? FirstPersonDefaultPriority 
            : TopPriority + 1;
        
        var characterState = Character.localCharacter.data;
        var playerIsClimbing = characterState != null && characterState.isClimbingAnything; 
        if (playerIsClimbing != _wasClimbing)
        {
            Cameras.FollowCamera.Priority = playerIsClimbing ? FollowDefaultPriority : TopPriority;
            Cameras.ClimbCamera.Priority = playerIsClimbing ? TopPriority : FollowDefaultPriority;
            _wasClimbing = playerIsClimbing;
        }
    }
}