using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using System.Linq.Expressions;
using UnityEngine;

namespace Linkoid.Peak.StableCamera
{
    [BepInPlugin("Linkoid.Peak.StableCamera", "StableCamera", "1.3")]
    public class StableCamera : BaseUnityPlugin
    {
        internal static StableCamera Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger => Instance._logger;
        private ManualLogSource _logger => base.Logger;
        internal Harmony? Harmony { get; set; }

        internal static new ConfigModel Config { get; private set; }

        private void Awake()
        {
            Instance = this;

            Config = new ConfigModel(base.Config);
            Config.RunMigrations();

            Patch();

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }

        internal void Patch()
        {
            Harmony ??= new Harmony(Info.Metadata.GUID);
            Harmony.PatchAll();
        }

        internal void Unpatch()
        {
            Harmony?.UnpatchSelf();
        }

        private void Update()
        {
            if (Input.GetKeyDown(Config.ToggleKey.Value))
            {
                Config.Enabled.Value = !Config.Enabled.Value;
                OnToggleEnabled(Config.Enabled.Value);
            }
        }

        private void OnToggleEnabled(bool enabled)
        {
            Logger.LogInfo($"Stable Camera Enabled: {Config.Enabled.Value}");

            QueueLogMessage($"<userColor>Stable Camera</color> has been {(enabled ? "<joinedColor>" : "<leftColor>")}{(enabled ? "enabled" : "disabled")}</color>");
        }

        public static void QueueLogMessage(string message, bool sfxJoin = false, bool sfxLeave = false, float delay = -1)
        {
            Instance.StartCoroutine(Instance.QueueLogMessageRoutine(message, sfxJoin, sfxLeave, delay));
        }

        private IEnumerator QueueLogMessageRoutine(string message, bool sfxJoin = false, bool sfxLeave = false, float delay = -1)
        {
            PlayerConnectionLog log;
            while ((log = Object.FindObjectOfType<PlayerConnectionLog>()) is null)
            {
                yield return null;
            }

            if (delay >= 0) yield return new WaitForSeconds(delay);

            message = message.Replace("<userColor>", log.GetColorTag(log.userColor));
            message = message.Replace("<joinedColor>", log.GetColorTag(log.joinedColor));
            message = message.Replace("<leftColor>", log.GetColorTag(log.leftColor));

            log.AddMessage(message);

            if (sfxJoin) log.sfxJoin?.Play();
            if (sfxLeave) log.sfxLeave?.Play();
        }
    }
}