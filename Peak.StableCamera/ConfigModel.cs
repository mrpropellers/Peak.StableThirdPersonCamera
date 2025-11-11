using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Linkoid.Peak.StableCamera;

internal readonly record struct ConfigModel(ConfigFile ConfigFile)
{
    public readonly ConfigEntry<bool>  Enabled             = ConfigFile.Bind("StableCamera", nameof(Enabled), true, "Enables the mod. Setting this to false effectively disables the entire mod.");
    public readonly ConfigEntry<KeyCode> ToggleKey         = ConfigFile.Bind("StableCamera", nameof(ToggleKey), KeyCode.N, "The shortcut key which toggles the mod on or off in-game.");
    public readonly ConfigEntry<bool>  StabilizeTracking   = ConfigFile.Bind("StableCamera", nameof(StabilizeTracking), true, "Reduce camera wobble using stabilized tracking. Disable this if head clipping is more annoying than wobble.");
    public readonly ConfigEntry<float> TrackingPower       = ConfigFile.Bind("StableCamera", nameof(TrackingPower), 2f, new ConfigDescription("How aggressively the camera will follow the character. Higher values lead to more wobble, but less clipping.", new AcceptableValueRange<float>(0.1f, 400.0f)));
    public readonly ConfigEntry<bool>  HideFullBody        = ConfigFile.Bind("StableCamera", nameof(HideFullBody), false, "Hide the entire body. Will cause your arms to be invisible in first person, but when TrackingPower is set to less than 1, this will prevent the camera from clipping with the player.");
    public readonly ConfigEntry<bool>  ThirdPersonRagdoll  = ConfigFile.Bind("StableCamera", nameof(ThirdPersonRagdoll), true, "Switch to a third-person camera whenever the character ragdolls.");
    public readonly ConfigEntry<float> ExtraClimbingFOV    = ConfigFile.Bind("StableCamera", nameof(ExtraClimbingFOV), 0f, new ConfigDescription("How much the camera's field of view expands while climbing. A value of 0 prevents the FOV from changing; 40 is the game's original value.", new AcceptableValueRange<float>(0.0f, 70.0f)));
    public readonly ConfigEntry<float> DizzyEffectStrength = ConfigFile.Bind("StableCamera", nameof(DizzyEffectStrength), 0f, new ConfigDescription("Strength factor of the dizzy camera effect, e.g. when recovering from passing out.", new AcceptableValueRange<float>(0.0f, 1.0f)));
    public readonly ConfigEntry<float> ShakeEffectStrength = ConfigFile.Bind("StableCamera", nameof(ShakeEffectStrength), 0f, new ConfigDescription("Strength factor of the camera shake effect, e.g. when stamina is exhausted while climbing.", new AcceptableValueRange<float>(0.0f, 1.0f)));

    public void RunMigrations()
    {
        ConfigFile.SaveOnConfigSet = false;
        try
        {
            Migrate_1_1();
            ConfigFile.Save();
        }
        catch (Exception ex)
        {
            StableCamera.Logger.LogWarning($"An error occured during config migration. Please delete the config file.\n{ex.Message}\n{ex.StackTrace}");
        }
        ConfigFile.SaveOnConfigSet = true;
    }

    private bool Migrate_1_1()
    {
        bool hasOldConfig = false;
        hasOldConfig |= ConfigFile.TryGetOrphanedEntry<bool>(new ConfigDefinition("StableCamera", "StabilizeCamera"), out var oldStabilizeCamera);

        if (!hasOldConfig) return false;

        StableCamera.Logger.LogMessage("Migrating config from v1.0 to v1.1");

        if (oldStabilizeCamera != null)
        {
            Enabled.Value = oldStabilizeCamera.Value;
        }

        ConfigFile.Remove(oldStabilizeCamera?.Definition);

        return true;
    }
}
