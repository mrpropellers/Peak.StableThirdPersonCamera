using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Linkoid.Peak.StableCamera;

#nullable disable

[HarmonyPatch]
[RequireComponent(typeof(Camera), typeof(MainCameraMovement))]
internal class MainCameraBodyHider : MonoBehaviour
{
    public static MainCameraBodyHider Instance { get; private set; }

    static readonly List<Renderer> reusableRendererList = new();

    private Camera camera;
    private MainCameraMovement mainCameraMovement;
    private HideTheBody playerBodyHider = null;

    [HarmonyPostfix, HarmonyPatch(typeof(MainCameraMovement), nameof(MainCameraMovement.Start))]
    static void CreateMainCameraBodyHider(MainCameraMovement __instance)
    {
        __instance.gameObject.AddComponent<MainCameraBodyHider>();
    }

    void Awake()
    {
        Instance = this;
        camera = this.GetComponent<Camera>();
        mainCameraMovement = this.GetComponent<MainCameraMovement>();
    }

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        //RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        //RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

        if (!TryGetPlayerBodyHider()) return;
        EnableHeadAndHats(true);
        EnableBodyAndCostumes(true);
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!TryGetPlayerBodyHider()) return;
        //StableCamera.Logger.LogDebug($"OnBeginCameraRendering({context}, {camera})");
        if (camera == this.camera)
        {
            ConditionallyDisableMeshRenderers();
        }
        else
        {
            EnableHeadAndHats(true);
            EnableBodyAndCostumes(true);
        }
    }

    private bool TryGetPlayerBodyHider()
    {
        if (playerBodyHider != null) return true;
        if (Character.localCharacter == null) return false;
        playerBodyHider = Character.localCharacter.GetComponentInChildren<HideTheBody>();
        return playerBodyHider != null;
    }

    private void ConditionallyDisableMeshRenderers()
    {
        // Roughly estimated value for when we're far enough into the ragdoll cam that we should start seeing the player body
        const float ragdollCamThreshold = 0.35f;

        // Turn body rendering off IF we are using the stable cam AND we're not passed out (putting us in 3rd person camera)
        var characterData = Character.localCharacter?.data;
        bool probablyInThirdPerson = false;
        if (characterData != null)
        {
            var probablyInRagdollCam = StableCamera.Config.ThirdPersonRagdoll.Value && mainCameraMovement.ragdollCam > ragdollCamThreshold;
            probablyInThirdPerson = probablyInRagdollCam || characterData.passedOut || characterData.fullyPassedOut || characterData.dead;
        }

        var shouldHideMeshes = StableCamera.Config.Enabled.Value && !probablyInThirdPerson;
        var shouldHideBody = shouldHideMeshes && StableCamera.Config.HideFullBody.Value;
        EnableHeadAndHats(!shouldHideMeshes);
        EnableBodyAndCostumes(!shouldHideBody);
    }

    private void EnableHeadAndHats(bool enabled)
    {
        playerBodyHider.headRend.enabled = enabled;
        foreach (var hatRenderer in playerBodyHider.refs.playerHats)
        {
            hatRenderer.enabled = enabled;
        }
        playerBodyHider.face.GetComponentsInChildren(reusableRendererList);
        foreach (var faceRenderer in reusableRendererList)
        {
            faceRenderer.enabled = enabled;
        }
    }

    private void EnableBodyAndCostumes(bool enabled)
    {
        playerBodyHider.body.enabled = enabled;
        playerBodyHider.sash.enabled = enabled;
        foreach (var costumeRenderer in playerBodyHider.costumes)
        {
            costumeRenderer.enabled = enabled;
        }
    }
}

#nullable restore
