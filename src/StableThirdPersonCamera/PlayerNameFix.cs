using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace StableThirdPersonCamera;

[HarmonyPatch(typeof(UIPlayerNames), nameof(UIPlayerNames.UpdateName))]
public class PlayerNameFix
{
    public static void Postfix(ref UIPlayerNames __instance, int index, Vector3 position, bool visible, int speakingAmplitude)
    {
        if (!StableThirdPersonCamera.Enabled || index >= __instance.playerNameText.Length)
            return;
        
        __instance.playerNameText[index].transform.position = Cameras.BrainCamera.WorldToScreenPoint(position);
      }
}
