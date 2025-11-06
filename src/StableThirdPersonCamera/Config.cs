using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace StableThirdPersonCamera;

public static class Settings
{
    static ConfigModel Cfg => StableThirdPersonCamera.Config;

    public static bool PutAudioListenerOnBody
        => Cfg.AdjustAudioSpatialization.Value;
}

internal readonly record struct ConfigModel(ConfigFile configFile)
{
    public readonly ConfigEntry<bool>  Enabled             = 
        configFile.Bind("StableCamera", nameof(Enabled), true, 
            "Enables third person mode. Note that turning this off mid-game will leave stabilization on. " +
            "To fully disable the mod you need to start a new run with this setting toggled off.");
    public readonly ConfigEntry<KeyCode> ToggleKey         = configFile.Bind("StableCamera", nameof(ToggleKey), KeyCode.N, "The shortcut key which toggles the mod on or off in-game.");
    public readonly ConfigEntry<float> TrackingPower       = configFile.Bind("StableCamera", nameof(TrackingPower), 0.5f, 
        new ConfigDescription("How aggressively the camera will follow the character. Higher values lead to more wobble, but less clipping.", new AcceptableValueRange<float>(0.1f, 400.0f)));

    public readonly ConfigEntry<float> WalkingCameraDistance = configFile.Bind("StableCamera",
        nameof(WalkingCameraDistance), 4f,
        new ConfigDescription(
            "How far away from the character the camera floats when not climbing. Setting this higher makes the game easier.",
        new AcceptableValueRange<float>(1f, 50f)));
    public readonly ConfigEntry<float> ClimbingCameraDistance = configFile.Bind("StableCamera",
        nameof(ClimbingCameraDistance), 4f,
        new ConfigDescription(
            "How far away from the character the camera floats when climbing. Setting this higher makes the game easier.",
            new AcceptableValueRange<float>(1f, 50f)));
    // TODO: Add some config binds for commonly tweaked Cinemachine parameters (collider radius, camera distance, etc.)
    public readonly ConfigEntry<bool> AdjustAudioSpatialization = configFile.Bind("StableCamera", nameof(AdjustAudioSpatialization),
        true, new ConfigDescription("PEAK's sound effects are mastered assuming the camera is inside the player.  " +
            "When the camera is farther from the player, the audio would be much quieter as a result. We correct for this by moving the camera's audio listener onto the player, " +
            "which keeps it sounding like it did before, but is a less realistic audio presentation. Set this to false for more 'realism,' but much quieter SFX."));
    public ConfigFile configFile { get; } = configFile;
}
