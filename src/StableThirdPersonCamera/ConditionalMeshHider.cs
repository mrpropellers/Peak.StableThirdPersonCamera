using UnityEngine;
using UnityEngine.Rendering;

namespace StableThirdPersonCamera;

public class ConditionalMeshHider : MonoBehaviour
{
    float _timeLastThrowing = 0f;
    float k_ShowHeadDelayAfterThrow = .75f;
    
    public static HideTheBody? PlayerBodyHider;
    
    public void Update()
    {
        if (!Cameras.HasSetUpCameras)
            return;
        
        if (PlayerBodyHider == null)
        {
            // Fetch this component because it has direct references to all the renderers we care about
            PlayerBodyHider = Character.localCharacter.GetComponentInChildren<HideTheBody>();
            if (PlayerBodyHider == null)
                return;
        }
        
        ConditionallyDisableMeshRenderers();
    }
    
    void ConditionallyDisableMeshRenderers()
    {
        var probablyInThirdPerson = Cameras.ProbablyInThirdPerson;
        
        if (Cameras.IsAimingThrow)
        {
            _timeLastThrowing = Time.time;
        }
        // We are good to hide these meshes any time we're not in third person, even though they usually are not
        // hidden without the mod installed.
        // (This might be why the scout book disappears? If it attaches to the head mesh when open...)
        bool shouldShowPotentiallyClippingMeshes = probablyInThirdPerson && Time.time - _timeLastThrowing > k_ShowHeadDelayAfterThrow;
        PlayerBodyHider.headRend.enabled = shouldShowPotentiallyClippingMeshes;
        PlayerBodyHider.sash.enabled = shouldShowPotentiallyClippingMeshes;
        foreach (var hatRenderer in PlayerBodyHider.refs.playerHats)
        {
            hatRenderer.enabled = shouldShowPotentiallyClippingMeshes;
        }

        // These meshes SHOULD show up in first-person view
        bool shouldShowNonClippingMeshes = !Cameras.IsAimingThrow && (probablyInThirdPerson || !StableThirdPersonCamera.Enabled);
        PlayerBodyHider.body.enabled = shouldShowNonClippingMeshes;
        foreach (var meshRenderer in PlayerBodyHider.costumes)
        {
            meshRenderer.enabled = shouldShowNonClippingMeshes;
        }
    }
}