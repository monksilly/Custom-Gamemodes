using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomGameModes.Patches;

partial class ModPatches
{
    [HarmonyPatch(typeof(CL_AssetManager), nameof(CL_AssetManager.UnloadAllLevels))]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    static bool BlockAddressableUnload()
    {
        return false;
    }
}

public static class LevelModifierCache
{
    public static readonly List<string> OrderedModifiers = [];
    public static readonly Regex ModifierRegex = new(@"(\b\w+?)\[([^\]]+)\]");
    
    // Tracks the current index of the level being natively initialized
    public static int CurrentInitializationIndex = 0;
}

[HarmonyPatch(typeof(CommandConsole))]
public static class CommandConsole_Patch
{
    [HarmonyPatch("ExecuteCommand"), HarmonyPrefix]
    public static void Prefix(ref string input)
    {
        if (string.IsNullOrEmpty(input) || !input.StartsWith("loadlevels", StringComparison.OrdinalIgnoreCase)) 
            return;

        LevelModifierCache.OrderedModifiers.Clear();

        MatchCollection matches = LevelModifierCache.ModifierRegex.Matches(input);
        foreach (Match match in matches)
        {
            string modifiers = match.Groups[2].Value;
            LevelModifierCache.OrderedModifiers.Add(modifiers);
        }

        input = LevelModifierCache.ModifierRegex.Replace(input, "$1");
    }
}

[HarmonyPatch(typeof(WorldLoader), "GenerateLevels", MethodType.Enumerator)]
public static class WorldLoader_GenerateLevels_Patch
{
    public static void Prefix()
    {
        LevelModifierCache.CurrentInitializationIndex = 0;
    }
}


[HarmonyPatch(typeof(M_Level), nameof(M_Level.Initialize))]
public static class M_Level_Initialize_Patch
{
    public static void Prefix(M_Level __instance)
    {
        if (__instance == null || __instance.gameObject == null) return;

        int index = LevelModifierCache.CurrentInitializationIndex;
        
        // Advance the tracker for the next structural component down the line
        LevelModifierCache.CurrentInitializationIndex++;

        // Only apply if the console command provided a modifier rule for this sequential index
        if (index >= LevelModifierCache.OrderedModifiers.Count) return;

        string mods = LevelModifierCache.OrderedModifiers[index];

        try
        {
            // Coordinate Logic [100 ~ ~]
            if (mods.Contains("~") || mods.Contains(" "))
            {
                string[] coordinates = mods.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                if (coordinates.Length == 3)
                {
                    // Let the native generator establish the baseline position using its Entrance/Exit logic first
                    Vector3 nativeGeneratedPos = __instance.transform.position;

                    // Add your custom padding adjustments on top of the native baseline
                    float x = coordinates[0] == "~" ? nativeGeneratedPos.x : nativeGeneratedPos.x + float.Parse(coordinates[0]);
                    float y = coordinates[1] == "~" ? nativeGeneratedPos.y : nativeGeneratedPos.y + float.Parse(coordinates[1]);
                    float z = coordinates[2] == "~" ? nativeGeneratedPos.z : nativeGeneratedPos.z + float.Parse(coordinates[2]);

                    __instance.transform.position = new Vector3(x, y, z);
                }
            }

            // Rotation Logic [r]
            if (mods.Equals("r", StringComparison.OrdinalIgnoreCase))
            {
                __instance.transform.rotation *= Quaternion.Euler(90f, 0f, 0f);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LevelModifier] Failed to alter layout offset at initialization step {index}: {ex.Message}");
        }
    }
}