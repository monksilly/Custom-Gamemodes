using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomGameModes.Patches
{
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
}
