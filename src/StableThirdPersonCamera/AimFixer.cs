using System;
using UnityEngine;

namespace StableThirdPersonCamera;

/// <summary>
/// Attempts to place the dummy camera that Peak uses for raycasts on the line projected forward
/// from the third person camera
/// </summary>
public class AimFixer : MonoBehaviour
{
    void Update()
    {
        var playerPosition = transform.parent.position;
        var cameraTf = Cameras.BrainCamera.transform;
        var cameraPosition = cameraTf.position;
        var cameraWayForward = cameraPosition + 100f * cameraTf.forward;
        var cameraLine = cameraWayForward - cameraPosition;
        var cameraToPlayer = playerPosition - cameraPosition;
        var tPlayerAlongCameraLine =
            Vector3.Dot(cameraLine, cameraToPlayer) / Vector3.Dot(cameraLine, cameraLine);
        transform.position = cameraPosition + Mathf.Clamp01(tPlayerAlongCameraLine) * cameraLine;
        transform.rotation = cameraTf.rotation;
    }
}
