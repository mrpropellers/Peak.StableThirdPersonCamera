using Linkoid.Peak.StableCamera;
using Unity.Cinemachine;
using UnityEngine;

public static class CinemachineTakeover
{
    public static int TopPriority = 9999;
    public static int FirstPersonDefaultPriority = 1;
    public static int ClimbDefaultPriority = 2;
    public static int FollowDefaultPriority = 3;
    public static bool WasClimbing = false;
    public static Camera BrainCamera;
    
    public static void SetUpComponents(Transform mainCameraTf)
    {
        StableCamera.LogToScreen("Setting up Cinemachine Takeover");
        var mainCam = MainCamera.instance;
        if (mainCam.transform != mainCameraTf)
        {
            StableCamera.LogToScreen("Uh oh. I thought these were the same!");
        }
        var cinemachineBrain = new GameObject("CinemachineBrain Camera");
        BrainCamera = cinemachineBrain.AddComponent<Camera>();
        BrainCamera.CopyFrom(mainCam.cam);
        cinemachineBrain.AddComponent<CinemachineBrain>();

        var firstPersonCam = new GameObject("FirstPersonCamera").AddComponent<CinemachineCamera>();
        firstPersonCam.Priority = FirstPersonDefaultPriority;
        firstPersonCam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin;
        firstPersonCam.Follow = mainCam.transform;
        firstPersonCam.LookAt = mainCam.transform;
        firstPersonCam.gameObject.AddComponent<CinemachineHardLockToTarget>();
        firstPersonCam.gameObject.AddComponent<CinemachineRotateWithFollowTarget>();
        mainCam.cam.enabled = false;
        FixCameraQuads();
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
        obstacles.CameraRadius = 1f;
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
        followThirdPerson.VerticalArmLength = 2;
        followThirdPerson.CameraSide = 1;
        followThirdPerson.CameraDistance = 5f;
        followThirdPerson.AvoidObstacles = obstacles;

        //climbCam = null;
        var climbGameObject = new GameObject("ThirdPersonClimbCamera");
        climbCam = climbGameObject.AddComponent<CinemachineCamera>();
        climbCam.Follow = follow;
        climbCam.Priority = ClimbDefaultPriority;
        climbCam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin;
        var climbThirdPerson = climbGameObject.AddComponent<CinemachineThirdPersonFollow>();
        climbThirdPerson.Damping = new Vector3(0.75f, 0.6f, 0.75f);
        climbThirdPerson.ShoulderOffset = new Vector3(0f, 0f, -0.5f);
        climbThirdPerson.CameraDistance = 8f;
        climbThirdPerson.AvoidObstacles = obstacles;
        
        followGameObject.AddComponent<MatchCameraProperties>();
        climbGameObject.AddComponent<MatchCameraProperties>();
    }
}
