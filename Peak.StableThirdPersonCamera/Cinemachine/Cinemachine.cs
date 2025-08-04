using Unity.Cinemachine;
using UnityEngine;

namespace Linkoid.Peak.StableCamera;
public static class Cinemachine
{
    public static Camera BrainCamera;
    // This will attach itself to the BrainCamera and follow it in lockstep, giving stuff that wants to change properties
    // that Cinemachine locks to its VCams a target. Then we'll copy them to our cameras
    public static Camera DummyCamera;
    // MainCameraMovement will drive this instead, which gives the CinemachineCameras a follow target that
    // mirrors the original
    public static CinemachineCamera FirstPersonCamera;
    public static HideTheBody? PlayerBodyHider;
    public static CinemachineCamera FollowCamera;
    public static CinemachineCamera ClimbCamera;
    
    public static int TopPriority = 9999;
    public static int FirstPersonDefaultPriority = 1;
    public static int ClimbDefaultPriority = 2;
    public static int FollowDefaultPriority = 3;
    public static bool WasClimbing = false;
    
    public static void SetUpComponents(GameObject mainCamera)
    {
        StableCamera.LogToScreen("Setting up Cinemachine Takeover");
        var mainCam = MainCamera.instance;
        if (mainCam.transform != mainCamera.transform)
        {
            StableCamera.LogToScreen("Uh oh. I thought these were the same!");
        }

        MainCameraMovementPatch.SubstituteTransform = new GameObject("New Camera Target").transform;
        var mainTf = mainCamera.transform;
        MainCameraMovementPatch.SubstituteTransform.SetPositionAndRotation(mainTf.position, mainTf.rotation);
        
        var dummyObject = new GameObject("DummyCamera");
        dummyObject.transform.SetParent(mainTf);
        DummyCamera = dummyObject.AddComponent<Camera>();
        DummyCamera.enabled = false;
        BrainCamera = mainCamera.GetComponent<Camera>();
        mainCamera.AddComponent<CinemachineBrain>();
        
        var firstPersonCam = new GameObject("FirstPersonCamera").AddComponent<CinemachineCamera>();
        firstPersonCam.Priority = FirstPersonDefaultPriority;
        firstPersonCam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin;
        firstPersonCam.Follow = MainCameraMovementPatch.SubstituteTransform;
        firstPersonCam.LookAt = MainCameraMovementPatch.SubstituteTransform;
        firstPersonCam.gameObject.AddComponent<CinemachineHardLockToTarget>();
        firstPersonCam.gameObject.AddComponent<CinemachineRotateWithFollowTarget>();
        FirstPersonCamera = firstPersonCam;
        var matchCamera = firstPersonCam.gameObject.AddComponent<MatchCameraProperties>();
        matchCamera.CameraToMatch = DummyCamera;
        //mainCam.cam.enabled = false;
        //mainCam.GetComponent<AudioListener>().enabled = false;
        //FixCameraQuads();
        
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
        var followCam = followGameObject.AddComponent<CinemachineCamera>();
        followCam.Follow = MainCameraMovementPatch.SubstituteTransform;
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
        followGameObject.AddComponent<MatchCameraProperties>().CameraToMatch = DummyCamera;
        FollowCamera = followCam;

        //climbCam = null;
        var climbGameObject = new GameObject("ThirdPersonClimbCamera");
        var climbCam = climbGameObject.AddComponent<CinemachineCamera>();
        climbCam.Follow = MainCameraMovementPatch.SubstituteTransform;
        climbCam.Priority = ClimbDefaultPriority;
        climbCam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin;
        climbCam.BlendHint |= CinemachineCore.BlendHints.InheritPosition;
        var climbThirdPerson = climbGameObject.AddComponent<CinemachineThirdPersonFollow>();
        climbThirdPerson.Damping = new Vector3(0.75f, 0.6f, 0.75f);
        climbThirdPerson.ShoulderOffset = new Vector3(0f, 0f, -0.5f);
        climbThirdPerson.CameraDistance = 8f;
        climbThirdPerson.AvoidObstacles = obstacles;
        climbGameObject.AddComponent<MatchCameraProperties>().CameraToMatch = DummyCamera;
        ClimbCamera = climbCam;

        var dummyMain = MainCameraMovementPatch.SubstituteTransform.gameObject.AddComponent<MainCamera>();
        dummyMain.cam = DummyCamera;
        BrainCamera.GetComponent<MainCamera>().enabled = false;
    }

    public static void UpdateVCamPriorities(bool playerIsClimbing)
    {
        if (playerIsClimbing != WasClimbing)
        {
            FollowCamera.Priority = playerIsClimbing ? FollowDefaultPriority : TopPriority;
            ClimbCamera.Priority = playerIsClimbing ? TopPriority : FollowDefaultPriority;
            Cinemachine.WasClimbing = playerIsClimbing;
            // TODO: I think I just need to call this one time about a second after this is originally set up
            //      And I'm probably safe to fully disable it rather than "fix" it
            Cinemachine.FixCameraQuads();
        }
        
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
            quad.cam = Linkoid.Peak.StableCamera.Cinemachine.BrainCamera;
        }
        if (quadsWereBroke)
            StableCamera.LogToScreen("Quads were broke again");
    }
}
