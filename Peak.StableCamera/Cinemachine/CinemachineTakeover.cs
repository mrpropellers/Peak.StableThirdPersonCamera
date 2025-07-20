using HarmonyLib;
using Linkoid.Peak.StableCamera;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[HarmonyPatch(typeof(Camera))]
public static class OverrideCameraMain
{
    [HarmonyPatch("main"), HarmonyPatch(MethodType.Getter), HarmonyPrefix]
    public static bool GetMain_Prefix(ref Camera __result)
    {
        if (StableCamera.Config.Enabled.Value && CinemachineTakeover.BrainCamera != null)
        {
            __result = CinemachineTakeover.BrainCamera;
            return false;
        }
        
        return true;
    }
}
// 
// [HarmonyPatch(typeof(MainCamera))]
// public static class OverrideMainCamera
// {
//     [HarmonyPatch("transform"), HarmonyPatch(MethodType.Getter), HarmonyPrefix]
//     public static bool GetTransform_Prefix(ref Transform __result)
//     {
//         if (StableCamera.Enabled && CinemachineTakeover.BrainCamera != null)
//         {
//             __result = CinemachineTakeover.BrainCamera.transform;
//             return false;
//         }
// 
//         return true;
//     }
// }


public static class CinemachineTakeover
{
    
    public static int TopPriority = 9999;
    public static int FirstPersonDefaultPriority = 1;
    public static int ClimbDefaultPriority = 2;
    public static int FollowDefaultPriority = 3;
    public static bool WasClimbing = false;
    public static Camera BrainCamera;
    public static CinemachineCamera FirstPersonCamera;
    
    public static void SetUpComponents(Transform mainCameraTf)
    {
        StableCamera.LogToScreen("Setting up Cinemachine Takeover");
        var mainCam = MainCamera.instance;
        if (mainCam.transform != mainCameraTf)
        {
            StableCamera.LogToScreen("Uh oh. I thought these were the same!");
        }

        // if (mainCam.TryGetComponent<UniversalAdditionalCameraData>(out var _))
        // {
        //     StableCamera.LogToScreen("Found Universal Camera data!");
        // }

        // >>> BAD BROKEN WAY I CAN'T FIGURE OUT BECAUSE MainCameraMovement self-destructs on Awake <<<
        // var cinemachineBrain = GameObject.Instantiate(mainCam).gameObject;
        // cinemachineBrain.name = "Cinemachine Brain Camera";
        // // Clear out the objects we don't actually want
        // Component.Destroy(cinemachineBrain.GetComponent<AudioListener>());
        // Component.Destroy(cinemachineBrain.GetComponent<MainCameraMovement>());
        // Component.Destroy(cinemachineBrain.GetComponent<MainCamera>());
        // cinemachineBrain.GetComponent<AudioListener>().enabled = false;
        // cinemachineBrain.GetComponent<MainCameraMovement>().enabled = false;
        // cinemachineBrain.GetComponent<MainCamera>().enabled = false;
        // Ensure the newly instantiated "singleton" doesn't hijack this reference
        //MainCamera.instance = mainCam;
        
        // >>> Less good way because we're not getting ALL The camera data for some reason
        // (and color grading isn't working)
        var cinemachineBrain = new GameObject("CinemachineBrain Camera");
        BrainCamera = cinemachineBrain.AddComponent<Camera>();
        var cameraData = cinemachineBrain.AddComponent<UniversalAdditionalCameraData>();
        BrainCamera.CopyFrom(mainCam.cam);
        var mainCamData = mainCam.cam.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = mainCamData.renderPostProcessing;
        cameraData.antialiasing = mainCamData.antialiasing;
        cameraData.antialiasingQuality = mainCamData.antialiasingQuality;
        cameraData.dithering = mainCamData.dithering;
        
        //mainCam.cam.GetUniversalAdditionalCameraData();
        cinemachineBrain.AddComponent<CinemachineBrain>();
        
        var firstPersonCam = new GameObject("FirstPersonCamera").AddComponent<CinemachineCamera>();
        firstPersonCam.Priority = FirstPersonDefaultPriority;
        firstPersonCam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin;
        firstPersonCam.Follow = mainCam.transform;
        firstPersonCam.LookAt = mainCam.transform;
        firstPersonCam.gameObject.AddComponent<CinemachineHardLockToTarget>();
        firstPersonCam.gameObject.AddComponent<CinemachineRotateWithFollowTarget>();
        FirstPersonCamera = firstPersonCam;
        var matchCamera = firstPersonCam.gameObject.AddComponent<MatchCameraProperties>();
        matchCamera.CameraToMatch = mainCam.cam;
        mainCam.cam.enabled = false;
        //mainCam.GetComponent<AudioListener>().enabled = false;
        //FixCameraQuads();
    }

    public static void FixCameraQuads()
    {
        // idk what this does but there sure are a lot of nullrefs if we don't do this
        var cameraQuads = Object.FindObjectsOfType<CameraQuad>();
        bool quadsWereBroke = false;
        foreach (var quad in cameraQuads)
        {
            if (quad.cam != null)
                continue;
            quadsWereBroke = true;
            quad.cam = BrainCamera;
        }
        if (quadsWereBroke)
            StableCamera.LogToScreen("Quads were broke again");
    }

    public static void CreateThirdPersonCamera(Transform follow, 
        out CinemachineCamera followCam, out CinemachineCamera climbCam)
    {
        var obstacles = CinemachineThirdPersonFollow.ObstacleSettings.Default;
        obstacles.Enabled = true;
        obstacles.CameraRadius = .5f;
        obstacles.DampingFromCollision = 0.75f;
        obstacles.DampingIntoCollision = 0.75f;
        obstacles.CollisionFilter = LayerMask.GetMask(new string[]
        {
            //"Default", 
            "Map"
        });
        
        var followGameObject = new GameObject("ThirdPersonFollowCamera");
        followCam = followGameObject.AddComponent<CinemachineCamera>();
        followCam.Follow = follow;
        //followCam.LookAt = follow;
        followCam.Priority = FollowDefaultPriority;
        followCam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin;
        var followThirdPerson = followGameObject.AddComponent<CinemachineThirdPersonFollow>();
        followThirdPerson.Damping = new Vector3(0.75f, 0.6f, 0.75f);
        followThirdPerson.VerticalArmLength = 1f;
        followThirdPerson.CameraSide = 1;
        followThirdPerson.CameraDistance = 5f;
        followThirdPerson.AvoidObstacles = obstacles;
        followCam.BlendHint |= CinemachineCore.BlendHints.InheritPosition;

        //climbCam = null;
        var climbGameObject = new GameObject("ThirdPersonClimbCamera");
        climbCam = climbGameObject.AddComponent<CinemachineCamera>();
        climbCam.Follow = follow;
        climbCam.Priority = ClimbDefaultPriority;
        climbCam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin;
        climbCam.BlendHint |= CinemachineCore.BlendHints.InheritPosition;
        var climbThirdPerson = climbGameObject.AddComponent<CinemachineThirdPersonFollow>();
        climbThirdPerson.Damping = new Vector3(0.75f, 0.6f, 0.75f);
        climbThirdPerson.ShoulderOffset = new Vector3(0f, 0f, -0.5f);
        climbThirdPerson.CameraDistance = 8f;
        climbThirdPerson.AvoidObstacles = obstacles;
        followGameObject.AddComponent<MatchCameraProperties>();
        climbGameObject.AddComponent<MatchCameraProperties>();
    }
}
