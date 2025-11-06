using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace StableThirdPersonCamera;

[BepInPlugin("LeftOut.Peak.StableThirdPersonCamera", "StableThirdPersonCamera", "1.0.0")]
public class StableThirdPersonCamera : BaseUnityPlugin
{
    internal static StableThirdPersonCamera Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    internal static new ConfigModel Config { get; private set; }
    public static bool Enabled => Config.Enabled.Value;

    private void Awake()
    {
        Instance = this;

        Config = new ConfigModel(base.Config);
        //Config.RunMigrations();

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
            OnToggleEnabled();
        }

        if (!Config.Enabled.Value)
            return;
        
        // TODO: Ensure camera config values stay up to date with in-game config changes
    }

    private void RefreshConfigImpl()
    {
        Config.configFile.Reload();
        //Config = new ConfigModel(base.Config);
    }
    public static void RefreshConfig()
    {
        Instance.RefreshConfigImpl();
    }

    private void OnToggleEnabled()
    {
        var wasEnabled = Config.Enabled.Value;
        Logger.LogInfo($"Stable Camera Enabled: {!wasEnabled}");
        RefreshConfig();
        Config.Enabled.Value = !wasEnabled;
        if (Config.Enabled.Value)
            Cameras.ApplyConfigValues();
        
        var log = Object.FindObjectOfType<PlayerConnectionLog>();
        if (log != null)
            log.AddMessage($"{log.GetColorTag(log.userColor)}Third Person Camera</color> has been {log.GetColorTag(!wasEnabled ? log.joinedColor : log.leftColor)}{(!wasEnabled ? "enabled" : "disabled")}</color>");
        
        //if (enabled && log.sfxJoin) log.sfxJoin.Play();
        //if (!enabled && log.sfxLeave) log.sfxLeave.Play();
    }

    static PlayerConnectionLog PeakLogger;

    public static void LogSetupSuccess()
    {
        if (PeakLogger == null)
            return;
        PeakLogger.AddMessage($"Third person cameras set up {PeakLogger.GetColorTag(PeakLogger.joinedColor)}Successfully</color>!");
    }
    
    public static void LogToScreen(string msg)
    {
        Logger.LogInfo(msg);
        if (PeakLogger == null)
            PeakLogger = FindObjectOfType<PlayerConnectionLog>();
        if (PeakLogger == null)
            return;
        PeakLogger.AddMessage(msg);
    }
}
