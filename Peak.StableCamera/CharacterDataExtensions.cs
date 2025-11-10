using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;
using MonoMod.Utils;

namespace Linkoid.Peak.StableCamera;

[HarmonyPatch]
internal static class CharacterDataExtensions
{
    static Func<CharacterData, float>? get_currentRagdollControll;

    [HarmonyPrepare]
    static void GetDynamicMethods(Harmony harmony)
    {
        try
        {
            ReversePatch_GetCurrentRagdollControll(harmony);
        }
        catch (Exception ex)
        {
            StableCamera.Logger.LogError(ex);
            StableCamera.QueueLogMessage(
                "<leftColor>An minor error has occured in <userColor>StableCamera</color>. The mod may not work as expected.</color>",
                sfxLeave: true,
                delay: 5
            );
        }
    }

    static void ReversePatch_GetCurrentRagdollControll(Harmony harmony)
    {
        // if
        MethodInfo propertyGetter = AccessTools.PropertyGetter(typeof(CharacterData), nameof(CharacterData.currentRagdollControll));
        if (propertyGetter is not null)
        {
            get_currentRagdollControll = propertyGetter.CreateDelegate<Func<CharacterData, float>>();
            return;
        }

        // else if
        var field_currentRagdollControll = AccessTools.Field(typeof(CharacterData), nameof(CharacterData.currentRagdollControll));
        if (field_currentRagdollControll is not null)
        {
            get_currentRagdollControll = CompileFieldGetter<CharacterData, float>(field_currentRagdollControll);
            return;
        }

        // else
        {
            throw new MissingMemberException($"Field or property '{nameof(CharacterData)}.{nameof(CharacterData.currentRagdollControll)}' not found.");
        }
    }

    /// <summary>
    /// currentRagdollControll was changed from a field to a property around v1.40.
    /// </summary>
    public static float GetCurrentRagdollControll(this CharacterData characterData)
    {
        return get_currentRagdollControll?.Invoke(characterData) ?? 1;
    }

    [return: NotNullIfNotNull(nameof(fieldInfo))]
    private static Func<TTarget, TResult>? CompileFieldGetter<TTarget, TResult>(FieldInfo? fieldInfo)
    {
        var target = Expression.Parameter(typeof(TTarget), "obj");
        var read_f = Expression.Field(target, fieldInfo);
        var getter = Expression.Lambda<Func<TTarget, TResult>>(read_f, target).Compile();
        return getter;
    }
}
