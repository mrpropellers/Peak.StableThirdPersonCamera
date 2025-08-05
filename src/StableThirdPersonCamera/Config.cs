using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace StableThirdPersonCamera;

internal readonly record struct ConfigModel(ConfigFile configFile)
{
    public readonly ConfigEntry<bool>  Enabled             = 
        configFile.Bind("StableCamera", nameof(Enabled), true, 
            "Enables third person mode. Note that turning this off mid-game will leave stabilization on. " +
            "To fully disable the mod you need to start a new run with this setting toggled off.");
    public readonly ConfigEntry<KeyCode> ToggleKey         = configFile.Bind("StableCamera", nameof(ToggleKey), KeyCode.N, "The shortcut key which toggles the mod on or off in-game.");
    //public readonly ConfigEntry<bool>  StabilizeTracking   = configFile.Bind("StableCamera", nameof(StabilizeTracking), true, "Reduce camera wobble using stabilized tracking. Disable this if head clipping is more annoying than wobble.");
    public readonly ConfigEntry<float> TrackingPower       = configFile.Bind("StableCamera", nameof(TrackingPower), 0.5f, new ConfigDescription("How aggressively the camera will follow the character. Higher values lead to more wobble, but less clipping.", new AcceptableValueRange<float>(0.1f, 400.0f)));
    // public readonly ConfigEntry<bool> HidePlayerMesh = configFile.Bind("StableCamera", nameof(HidePlayerMesh), false,
    //     new ConfigDescription("Whether or not the player's mesh will be rendered. Will cause your arms to be invisible in first person, " +
    //         "but when TrackingPower is set to less than 1, this will prevent the camera from clipping with the player."));
    //public readonly ConfigEntry<bool>  ThirdPersonRagdoll  = configFile.Bind("StableCamera", nameof(ThirdPersonRagdoll), true, "Switch to a third-person camera whenever the character ragdolls.");
    public readonly ConfigEntry<float> ExtraClimbingFOV    = configFile.Bind("StableCamera", nameof(ExtraClimbingFOV), 0f, new ConfigDescription("How much the camera's field of view expands while climbing. A value of 0 prevents the FOV from changing; 40 is the game's original value.", new AcceptableValueRange<float>(0.0f, 70.0f)));
    // public readonly ConfigEntry<float> DizzyEffectStrength = configFile.Bind("StableCamera", nameof(DizzyEffectStrength), 0f, new ConfigDescription("Strength factor of the dizzy camera effect, e.g. when recovering from passing out.", new AcceptableValueRange<float>(0.0f, 1.0f)));
    // public readonly ConfigEntry<float> ShakeEffectStrength = configFile.Bind("StableCamera", nameof(ShakeEffectStrength), 0f, new ConfigDescription("Strength factor of the camera shake effect, e.g. when stamina is exhausted while climbing.", new AcceptableValueRange<float>(0.0f, 1.0f)));
    public ConfigFile configFile { get; } = configFile;

    public void RunMigrations()
    {
        configFile.SaveOnConfigSet = false;
        try
        {
            Migrate_1_1();
            configFile.Save();
        }
        catch (Exception ex)
        {
            StableThirdPersonCamera.Logger.LogWarning($"An error occured during config migration. Please delete the config file.\n{ex.Message}\n{ex.StackTrace}");
        }
        configFile.SaveOnConfigSet = true;
    }

    private bool Migrate_1_1()
    {
        bool hasOldConfig = false;
        hasOldConfig |= TryGetOrphanedEntry<bool>(configFile, new ConfigDefinition("StableCamera", "StabilizeCamera"), out var oldStabilizeCamera);

        if (!hasOldConfig) return false;

        StableThirdPersonCamera.Logger.LogMessage("Migrating config from v1.0 to v1.1");

        if (oldStabilizeCamera != null)
        {
            Enabled.Value = oldStabilizeCamera.Value;
        }

        configFile.Remove(oldStabilizeCamera?.Definition);

        return true;
    }
    
    static readonly MethodInfo prop_OrphanedEntries = AccessTools.DeclaredPropertyGetter(typeof(ConfigFile), "OrphanedEntries");
    
    public static IReadOnlyCollection<ConfigDefinition> GetOrphanedDefinitions(ConfigFile configFile)
    {
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)prop_OrphanedEntries.Invoke(configFile, Array.Empty<object>());
        return orphanedEntries.Keys;
    }

    public static bool TryGetOrphanedEntry<T>(ConfigFile configFile, ConfigDefinition configDefinition, [NotNullWhen(true)] out ConfigEntry<T>? entry, T defaultValue = default!, ConfigDescription? configDescription = null)
    {
        entry = null;
        if (!GetOrphanedDefinitions(configFile).Contains(configDefinition))
            return false;

        entry = configFile.Bind(configDefinition, defaultValue, configDescription);
        return true;
    }
}
