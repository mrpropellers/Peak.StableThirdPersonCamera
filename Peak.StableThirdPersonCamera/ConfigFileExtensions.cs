using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Linkoid.Peak.StableCamera;

internal static class ConfigFileExtensions
{
    private static readonly MethodInfo prop_OrphanedEntries = AccessTools.DeclaredPropertyGetter(typeof(ConfigFile), "OrphanedEntries");

    public static IReadOnlyCollection<ConfigDefinition> GetOrphanedDefinitions(this ConfigFile configFile)
    {
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)prop_OrphanedEntries.Invoke(configFile, Array.Empty<object>());
        return orphanedEntries.Keys;
    }

    public static bool TryGetOrphanedEntry<T>(this ConfigFile configFile, ConfigDefinition configDefinition, [NotNullWhen(true)] out ConfigEntry<T>? entry, T defaultValue = default!, ConfigDescription? configDescription = null)
    {
        entry = null;
        if (!configFile.GetOrphanedDefinitions().Contains(configDefinition))
            return false;

        entry = configFile.Bind(configDefinition, defaultValue, configDescription);
        return true;
    }

    public static bool TryGetOrphanedEntry<T>(this ConfigFile configFile, string section, string key, [NotNullWhen(true)] out ConfigEntry<T>? entry, T defaultValue = default!, ConfigDescription? configDescription = null)
    {
        return configFile.TryGetOrphanedEntry(new ConfigDefinition(section, key), out entry, defaultValue, configDescription);
    }

    public static bool TryGetOrphanedEntry<T>(this ConfigFile configFile, string section, string key, [NotNullWhen(true)] out ConfigEntry<T>? entry, T defaultValue, string description)
    {
        return configFile.TryGetOrphanedEntry(new ConfigDefinition(section, key), out entry, defaultValue, new ConfigDescription(description, null));
    }
}
