#nullable disable
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System;
using System.Reflection;

namespace AnimalRespawnTimeMod
{
    public class AnimalRespawnTimeModMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            AnimalRespawnTimeSettings.OnLoad();
            DebugHelper.Init();
            EAFCompat.TryPatch(this.HarmonyInstance);
            MelonLogger.Msg("[AnimalRespawnTimeMod] Initialized!");
        }

        public static bool TryGetConfiguredHours(string prefabName, out float hours)
        {
            hours = 0f;
            if (string.IsNullOrEmpty(prefabName))
                return false;
            if (prefabName.EndsWith("(Clone)", StringComparison.OrdinalIgnoreCase))
            {
                prefabName = prefabName.Substring(0, prefabName.Length - "(Clone)".Length).Trim();
            }
            AnimalRespawnTimeSettings settings = AnimalRespawnTimeSettings.Instance;
            if (settings == null)
                return false;
            float days;
            switch (prefabName)
            {
                case "WILDLIFE_Wolf": days = settings.Wolfnumber; break;
                case "WILDLIFE_Bear": days = settings.Bearnumber; break;
                case "WILDLIFE_Rabbit": days = settings.Rabbitnumber; break;
                case "WILDLIFE_Doe": days = settings.Doenumber; break;
                case "WILDLIFE_Stag": days = settings.Stagnumber; break;
                case "WILDLIFE_Moose": days = settings.Moosenumber; break;
                case "WILDLIFE_Ptarmigan": days = settings.Ptarmigannumber; break;
                case "WILDLIFE_Wolf_grey": days = settings.Wolf_greynumber; break;
                case "WILDLIFE_Wolf_Starving": days = settings.Wolf_Starvingnumber; break;
                default:
                    return false;
            }
            hours = days * 24f;
            return true;
        }
    }

    // Temporary context used while RemoveFromSpawnRegion calculates the cooldown.
    internal static class RespawnPatchContext
    {
        public static string CurrentPrefabName;
    }

    // Capture the animal being removed from the spawn region.
    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.RemoveFromSpawnRegion))]
    public static class SpawnRegion_RemoveFromSpawnRegion_Patch
    {
        public static void Prefix(BaseAi __0, out string __state)
        {
            __state = RespawnPatchContext.CurrentPrefabName;
            RespawnPatchContext.CurrentPrefabName = null;

            if (__0 == null)
            {
                return;
            }

            try
            {
                string instanceName = __0.name;

                if (string.IsNullOrEmpty(instanceName))
                {
                    return;
                }

                RespawnPatchContext.CurrentPrefabName = SpawnRegion.GetPrefabNameFromInstanceName(instanceName);
            }
            catch (Exception ex)
            {
                DebugHelper.Log("[AnimalRespawnTimeMod] Failed to identify removed animal: " + ex);
            }
        }
        public static void Postfix(string __state)
        {
            // Set back the previously used context, if there are any
            RespawnPatchContext.CurrentPrefabName = __state;
        }
    }

    // Completely override the vanilla respawn interval during removal.
    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.GetNumHoursBetweenRespawns))]
    public static class SpawnRegion_GetNumHoursBetweenRespawns_Patch
    {
        public static void Postfix(ref float __result)
        {
            string prefabName = RespawnPatchContext.CurrentPrefabName;

            if (string.IsNullOrEmpty(prefabName))
            {
                return;
            }

            float customHours;

            if (!AnimalRespawnTimeModMain.TryGetConfiguredHours(
                prefabName,
                out customHours))
            {
                DebugHelper.Log("[AnimalRespawnTimeMod] No custom respawn interval for: " + prefabName);
                return;
            }

            __result = customHours;
            DebugHelper.Log("[AnimalRespawnTimeMod] Respawn interval overridden for " + prefabName + ": " + customHours + " hours.");
        }
    }
    internal static class EAFCompat
    {
        private static PropertyInfo s_VanillaSpawnRegionProperty;
        private static readonly Dictionary<int, string> s_LastKilledPrefabByRegion = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> s_FirstEntryPrefabByRegion = new Dictionary<int, string>();

        public static bool IsActive { get; private set; }

        public static void TryPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                Type customSpawnRegionType = AccessTools.TypeByName("ExpandedAiFramework.CustomSpawnRegion");
                if (customSpawnRegionType == null)
                {
                    IsActive = false;
                    MelonLogger.Msg("[AnimalRespawnTimeMod] EAF not detected. Using vanilla respawn handling.");
                    return;
                }

                s_VanillaSpawnRegionProperty = AccessTools.Property(customSpawnRegionType, "VanillaSpawnRegion");
                MethodInfo removeMethod = AccessTools.Method(customSpawnRegionType, "RemoveFromSpawnRegion", new Type[] { typeof(BaseAi) });
                MethodInfo getHoursMethod = AccessTools.Method(customSpawnRegionType, "GetNumHoursBetweenRespawns");
                if (s_VanillaSpawnRegionProperty == null || removeMethod == null || getHoursMethod == null)
                {
                    MelonLogger.Error("[AnimalRespawnTimeMod] EAF detected, but VanillaSpawnRegion / RemoveFromSpawnRegion / GetNumHoursBetweenRespawns was not found - did the EAF API change?");
                    return;
                }

                harmony.Patch(
                    removeMethod,
                    prefix: new HarmonyMethod(typeof(EAFCompat), nameof(RemoveFromSpawnRegionPrefix)),
                    finalizer: new HarmonyMethod(typeof(EAFCompat), nameof(RemoveFromSpawnRegionFinalizer)));
                harmony.Patch(
                    getHoursMethod,
                    postfix: new HarmonyMethod(typeof(EAFCompat), nameof(GetNumHoursBetweenRespawnsPostfix)));

                IsActive = true;
                MelonLogger.Msg("[AnimalRespawnTimeMod] EAF compatibility installed.");
            }
            catch (Exception ex)
            {
                IsActive = false;
                MelonLogger.Error("[AnimalRespawnTimeMod] Failed to patch EAF: " + ex);
            }
        }

        // Runs right before EAF removes the animal and computes the cooldown.
        public static void RemoveFromSpawnRegionPrefix(object __instance, BaseAi __0, out string __state)
        {
            __state = RespawnPatchContext.CurrentPrefabName;
            RespawnPatchContext.CurrentPrefabName = null;
            if (__0 == null)
                return;
            try
            {
                string instanceName = __0.name;
                if (string.IsNullOrEmpty(instanceName))
                    return;
                string prefabName = SpawnRegion.GetPrefabNameFromInstanceName(instanceName);
                RespawnPatchContext.CurrentPrefabName = prefabName;

                SpawnRegion region = GetVanillaRegion(__instance);
                if (region != null && !string.IsNullOrEmpty(prefabName))
                    s_LastKilledPrefabByRegion[region.GetInstanceID()] = prefabName;
            }
            catch (Exception ex)
            {
                DebugHelper.Log("[AnimalRespawnTimeMod] [EAF] Failed to identify removed animal: " + ex);
            }
        }
        // Finalizer (not Postfix) so the context is restored even if EAF's method throws.
        public static Exception RemoveFromSpawnRegionFinalizer(string __state, Exception __exception)
        {
            RespawnPatchContext.CurrentPrefabName = __state;
            return __exception;
        }

        public static void GetNumHoursBetweenRespawnsPostfix(object __instance, ref float __result)
        {
            try
            {
                string prefabName = RespawnPatchContext.CurrentPrefabName;
                if (string.IsNullOrEmpty(prefabName))
                    prefabName = ResolveRegionPrefabName(GetVanillaRegion(__instance));
                if (string.IsNullOrEmpty(prefabName))
                    return;

                float customHours;
                if (!AnimalRespawnTimeModMain.TryGetConfiguredHours(prefabName, out customHours))
                {
                    DebugHelper.Log("[AnimalRespawnTimeMod] [EAF] No custom respawn interval for: " + prefabName);
                    return;
                }
                __result = customHours;
                DebugHelper.Log("[AnimalRespawnTimeMod] [EAF] Respawn interval overridden for " + prefabName + ": " + customHours + " hours.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AnimalRespawnTimeMod] [EAF] GetNumHoursBetweenRespawnsPostfix failed: " + ex);
            }
        }

        // Fallback when there is no kill context: last killed species of this region,
        // otherwise the first spawn list entry (same source EAF itself uses for its prefab name).
        private static string ResolveRegionPrefabName(SpawnRegion region)
        {
            if (region == null)
                return null;
            int key = region.GetInstanceID();
            string prefabName;
            if (s_LastKilledPrefabByRegion.TryGetValue(key, out prefabName))
                return prefabName;
            if (s_FirstEntryPrefabByRegion.TryGetValue(key, out prefabName))
                return prefabName;

            Il2CppSystem.Collections.Generic.List<SpawnRegion.SpawnListEntry> spawnList = region.m_SpawnList;
            if (spawnList == null || spawnList.Count == 0)
                return null;
            AssetReferenceBaseAi reference = spawnList[0].GetAnimalForMode(region.m_WildlifeMode);
            if (reference == null)
                return null;
            BaseAi prefabAi = reference.GetOrLoadTypedAsset();
            if (prefabAi == null)
                return null;
            prefabName = prefabAi.gameObject.name;
            s_FirstEntryPrefabByRegion[key] = prefabName;
            return prefabName;
        }
        private static SpawnRegion GetVanillaRegion(object instance)
        {
            if (instance == null || s_VanillaSpawnRegionProperty == null)
                return null;
            return s_VanillaSpawnRegionProperty.GetValue(instance) as SpawnRegion;
        }
    }
}