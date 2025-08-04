using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Linkoid.Peak.StableCamera;

[HarmonyPatch(typeof(UIPlayerNames), nameof(UIPlayerNames.UpdateName))]
public class PlayerNameFix
{
    public static bool Prefix(ref UIPlayerNames __instance, int index, Vector3 position, bool visible, int speakingAmplitude)
    {
        if (!StableCamera.Enabled)
            return true;
        
        if (index >= __instance.playerNameText.Length)
          return false;
        __instance.playerNameText[index].transform.position = MainCamera.instance.cam.WorldToScreenPoint(position);
        if (visible)
        {
          __instance.playerNameText[index].gameObject.SetActive(true);
          __instance.playerNameText[index].group.alpha = Mathf.MoveTowards(__instance.playerNameText[index].group.alpha, 1f, Time.deltaTime * 5f);
          if ((bool) (Object) __instance.playerNameText[index].characterInteractable && (double) AudioLevels.GetPlayerLevel(__instance.playerNameText[index].characterInteractable.character.photonView.OwnerActorNr) == 0.0)
            __instance.playerNameText[index].audioImage.sprite = __instance.mutedAudioSprite;
          else if (speakingAmplitude <= 0)
          {
            __instance.playerNameText[index].audioImageTimeout -= Time.deltaTime;
            if ((double) __instance.playerNameText[index].audioImageTimeout > 0.0)
              return false;
            __instance.playerNameText[index].audioImage.sprite = __instance.audioSprites[0];
          }
          else
          {
            __instance.playerNameText[index].audioImage.sprite = __instance.audioSprites[Mathf.Clamp(speakingAmplitude, 0, __instance.audioSprites.Length - 1)];
            __instance.playerNameText[index].audioImageTimeout = __instance.audioImageTimeoutMax;
          }
        }
        else
        {
          __instance.playerNameText[index].group.alpha = Mathf.MoveTowards(__instance.playerNameText[index].group.alpha, 0.0f, Time.deltaTime * 5f);
          if ((double) __instance.playerNameText[index].group.alpha >= 0.009999999776482582 || !__instance.playerNameText[index].gameObject.activeSelf)
            return false;
          __instance.playerNameText[index].gameObject.SetActive(false);
        }

        return false;
      }
}
