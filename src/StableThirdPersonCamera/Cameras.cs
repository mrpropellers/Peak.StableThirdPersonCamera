using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

namespace StableThirdPersonCamera;
public static class Cameras
{
    // Roughly estimated value for when we're far enough into the ragdoll cam that we should start seeing the player body
    const float ragdollCamThreshold = 0.35f;
    const float throwChargeThreshold = 0.075f;

    static bool CharacterExists => !ReferenceEquals(null, Character.localCharacter);
    public static Camera BrainCamera;
    static CinemachineBrain Brain;
    static float CameraBlendAmount => Brain.ActiveBlend.TimeInBlend / Brain.ActiveBlend.Duration;

    public static bool ShouldSetUpCameras => StableThirdPersonCamera.Enabled && BrainCamera == null;

    public static bool IsStartingThrowCharge
        => CharacterExists
           && Character.localCharacter.refs.items.throwChargeLevel > 0.075f;

    public static bool IsAimingThrow
        => CharacterExists && Character.localCharacter.refs.items.throwChargeLevel > 0.75f;
    
    public static bool ProbablyInThirdPerson
    {
        get
        {
            if (Character.localCharacter == null)
                return false;
            var data = Character.localCharacter.data;
            var modIsInThirdPerson =
                StableThirdPersonCamera.Enabled;
            var probablyRagdolling = PeakCameraMover.ragdollCam > ragdollCamThreshold;
            var probablySpectating = !data.fullyConscious || Character.localCharacter.IsGhost;
            return modIsInThirdPerson || probablyRagdolling || probablySpectating;
        }
    }

    // TODO: Rather than null-checking over and over again we could hook into SceneManager callbacks and reset
    //      this value whenever the Scene changes
    public static bool HasSetUpCameras => BrainCamera != null;
    // This will attach itself to the BrainCamera and follow it in lockstep, giving stuff that wants to change properties
    // that Cinemachine locks to its VCams a target. Then we'll copy them to our cameras
    public static Camera DummyCamera;
    public static MainCameraMovement PeakCameraMover { get; private set; }
    public static CinemachineCamera FirstPersonCamera;
    public static ConditionalMeshHider MeshHider;
    public static CinemachineThirdPersonFollow FollowCamera;
    public static CinemachineThirdPersonFollow ClimbCamera;
    public static CinemachineCamera AimCamera;
    
    internal static CinemachineThirdPersonFollow.ObstacleSettings Default => new CinemachineThirdPersonFollow.ObstacleSettings()
    {
        Enabled = false,
        CollisionFilter = (LayerMask) 1,
        IgnoreTag = string.Empty,
        CameraRadius = 0.5f,
        DampingIntoCollision = 0.0f,
        DampingFromCollision = 0.5f
    };

    public static void ApplyConfigValues()
    {
        if (!HasSetUpCameras)
            return;
        var cameraDistance = StableThirdPersonCamera.Config.WalkingCameraDistance.Value;
        FollowCamera.CameraDistance = cameraDistance;
        FollowCamera.VerticalArmLength = Mathf.Sin(12f * Mathf.Deg2Rad) * cameraDistance;
        ClimbCamera.CameraDistance = StableThirdPersonCamera.Config.ClimbingCameraDistance.Value;
    }
    
    public static void SetUpComponents(GameObject mainCamera)
    {
        StableThirdPersonCamera.LogToScreen("Setting up third person cameras...");
        StableThirdPersonCamera.RefreshConfig();
        var mainCam = MainCamera.instance;
        if (mainCam.transform != mainCamera.transform)
        {
            StableThirdPersonCamera.LogToScreen("Uh oh. I thought these were the same!");
        }

        PeakCameraMover = mainCamera.GetComponent<MainCameraMovement>();
        if (!MeshHider)
        {
            MeshHider = new GameObject("Mesh Hider").AddComponent<ConditionalMeshHider>();
        }
        
        BrainCamera = mainCamera.GetComponent<Camera>();
        Brain = mainCamera.AddComponent<CinemachineBrain>();
        mainCamera.AddComponent<CameraPrioritySetter>();
        
        MainCameraMovementPatch.SubstituteTransform = new GameObject("New Camera Target").transform;
        var mainTf = mainCamera.transform;
        MainCameraMovementPatch.SubstituteTransform.SetPositionAndRotation(mainTf.position, mainTf.rotation);
        var dummyObject = new GameObject("DummyCamera");
        dummyObject.transform.SetParent(MainCameraMovementPatch.SubstituteTransform);
        // We want the fake MainCamera to be in the same position as the player head, but in the same orientation as
        // the current camera, in case someone is raycasting from the transform instead of from the camera it references
        dummyObject.AddComponent<AimFixer>();
        DummyCamera = dummyObject.AddComponent<Camera>();
        DummyCamera.enabled = false;
        var dummyMain = MainCameraMovementPatch.SubstituteTransform.gameObject.AddComponent<MainCamera>();
        dummyMain.cam = DummyCamera;
        BrainCamera.GetComponent<MainCamera>().enabled = false;
        // CinemachineBrain is going to take control of the camera properties for the MainCam, but MainCameraMovement
        // ALSO wants to set some camera properties, so we point MainCameraMovement at the DummyCamera and then copy
        // those properties over late
        
        var firstPersonCam = new GameObject("FirstPersonCamera").AddComponent<CinemachineCamera>();
        firstPersonCam.Priority = CameraPrioritySetter.FirstPersonDefaultPriority;
        firstPersonCam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin;
        firstPersonCam.Follow = MainCameraMovementPatch.SubstituteTransform;
        firstPersonCam.LookAt = MainCameraMovementPatch.SubstituteTransform;
        firstPersonCam.gameObject.AddComponent<CinemachineHardLockToTarget>();
        firstPersonCam.gameObject.AddComponent<CinemachineRotateWithFollowTarget>();
        firstPersonCam.BlendHint = CinemachineCore.BlendHints.InheritPosition;
        FirstPersonCamera = firstPersonCam;
        var matchCamera = firstPersonCam.gameObject.AddComponent<MatchCameraProperties>();
        matchCamera.CameraToMatch = DummyCamera;
        //mainCam.cam.enabled = false;
        //mainCam.GetComponent<AudioListener>().enabled = false;
        //FixCameraQuads();
        
        var obstacles = Default;
        obstacles.Enabled = true;
        obstacles.CameraRadius = .2f;
        obstacles.DampingFromCollision = 0.75f;
        obstacles.DampingIntoCollision = 0.75f;
        obstacles.CollisionFilter = LayerMask.GetMask(new string[]
        {
            //"Default", 
            "Map",
            "Terrain"
        });
        
        var followGameObject = new GameObject("ThirdPersonFollowCamera");
        var followCam = followGameObject.AddComponent<CinemachineCamera>();
        followCam.Follow = MainCameraMovementPatch.SubstituteTransform;
        //followCam.LookAt = follow;
        followCam.Priority = CameraPrioritySetter.FollowDefaultPriority;
        followCam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin;
        var followThirdPerson = followGameObject.AddComponent<CinemachineThirdPersonFollow>();
        followThirdPerson.Damping = new Vector3(0.75f, 0.6f, 0.75f);
        followThirdPerson.CameraSide = 1;
        followThirdPerson.AvoidObstacles = obstacles;
        followCam.BlendHint = CinemachineCore.BlendHints.InheritPosition;
        followGameObject.AddComponent<MatchCameraProperties>().CameraToMatch = DummyCamera;
        FollowCamera = followThirdPerson;

        //climbCam = null;
        var climbGameObject = new GameObject("ThirdPersonClimbCamera");
        var climbCam = climbGameObject.AddComponent<CinemachineCamera>();
        climbCam.Follow = MainCameraMovementPatch.SubstituteTransform;
        climbCam.Priority = CameraPrioritySetter.ClimbDefaultPriority;
        climbCam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin;
        climbCam.BlendHint = CinemachineCore.BlendHints.InheritPosition;
        var climbThirdPerson = climbGameObject.AddComponent<CinemachineThirdPersonFollow>();
        climbThirdPerson.Damping = new Vector3(0.75f, 0.6f, 0.75f);
        climbThirdPerson.ShoulderOffset = new Vector3(0f, -0.2f, -0.5f);
        climbThirdPerson.AvoidObstacles = obstacles;
        climbGameObject.AddComponent<MatchCameraProperties>().CameraToMatch = DummyCamera;
        ClimbCamera = climbThirdPerson;

        var aimAvoidance = obstacles;
        aimAvoidance.DampingIntoCollision = 0.05f;
        aimAvoidance.CameraRadius = 0.3f;
        
        var aimGameObject = new GameObject("ThirdPersonAimCamera");
        var aimCam = aimGameObject.AddComponent<CinemachineCamera>();
        aimCam.Follow = MainCameraMovementPatch.SubstituteTransform;
        //aimCam.LookAt = follow;
        aimCam.Priority = CameraPrioritySetter.FirstPersonDefaultPriority;
        aimCam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin;
        var aimThirdPerson = aimGameObject.AddComponent<CinemachineThirdPersonFollow>();
        aimThirdPerson.Damping = new Vector3(0.1f, 0.1f, 0.1f);
        aimThirdPerson.VerticalArmLength = 0f;
        aimThirdPerson.CameraSide = 1;
        aimThirdPerson.CameraDistance = 1.5f;
        aimThirdPerson.AvoidObstacles = aimAvoidance;
        aimCam.BlendHint = CinemachineCore.BlendHints.InheritPosition;
        aimGameObject.AddComponent<MatchCameraProperties>().CameraToMatch = DummyCamera;
        AimCamera = aimCam;
        
        ApplyConfigValues();

        var toAimBlend = new CinemachineBlenderSettings.CustomBlend
        {
            To = aimCam.name, 
            Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.HardIn, .6f)
        };
        var fromAimBlend = new CinemachineBlenderSettings.CustomBlend
        {
            From = aimCam.name, 
            Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.HardOut, .2f)
        };
        var newBlends = Brain.CustomBlends.CustomBlends.Append(toAimBlend);
        newBlends = newBlends.Append(fromAimBlend);
        // TODO: Need to confirm this is actually doing something... not convinced it is
        Brain.CustomBlends.CustomBlends = newBlends.ToArray();
        Brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1f);
        StableThirdPersonCamera.LogSetupSuccess();
    }
}
