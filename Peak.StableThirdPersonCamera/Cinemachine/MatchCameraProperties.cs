using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Linkoid.Peak.StableCamera;

public class MatchCameraProperties : CinemachineExtension
{
    public Camera CameraToMatch;
    
    public override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Aim)
            return;
        if (CameraToMatch == null)
        {
            CameraToMatch = MainCamera.instance?.cam;
            if (CameraToMatch == null)
                return;
        }

        // Copy whatever matters into here. I figure it's probably just FoV?
        state.Lens.FieldOfView = CameraToMatch.fieldOfView;
    }
}
