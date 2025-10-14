using System;
using UnityEngine;

namespace StableThirdPersonCamera;

public class AudioListenerToggler : MonoBehaviour
{
    AudioListener _body;
    AudioListener _camera;

    void Start()
    {
        if (!MainCamera.instance?.TryGetComponent(out _camera) ?? false)
        {
            StableThirdPersonCamera.LogToScreen($"No {nameof(AudioListener)} on {nameof(MainCamera)}! " +
                $"That's probably going to cause a lot of NREs...");
        }

        // The SubstituteTransform tracks the player's head in place of the camera's tf when we're doing 3rd person stuff
        _body = MainCameraMovementPatch.SubstituteTransform.gameObject.AddComponent<AudioListener>();
        _body.enabled = false;
    }

    void Update()
    {
        if (!Cameras.HasSetUpCameras || ReferenceEquals(null, Character.localCharacter))
            return;

        var shouldListenOnCamera = 
            !Settings.PutAudioListenerOnBody 
            || Character.localCharacter.IsGhost;

        _body.enabled = !shouldListenOnCamera;
        _camera.enabled = shouldListenOnCamera;
    }
}
