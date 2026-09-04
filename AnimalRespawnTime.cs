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

    // ============================================================
    // VANILLA
    //
    // A single Postfix is enough: SpawnRegion.RemoveFromSpawnRegion (native)
    // internally calls GetNumHoursBetweenRespawns() to compute
    // m_ElapasedHoursNextRespawnAllowed, and m_NumHoursBetweenRespawns /
    // m_ElapasedHoursNextRespawnAllowed / m_NumRespawnsPending are all
    // regular Il2Cpp instance fields, natively serialized by
    // SpawnRegion.Serialize()/Deserialize() - no ModData needed.
    // ============================================================
    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.GetNumHoursBetweenRespawns))]
    public static class SpawnRegion_GetNumHoursBetweenRespawns_Patch
    {
        public static void Postfix(SpawnRegion __instance, ref float __result)
        {
            if (__instance == null)
                return;
            string prefabName;
            try
            {
                prefabName = __instance.GetSpawnablePrefabName();
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AnimalRespawnTimeMod] Vanilla GetNumHoursBetweenRespawns failed: " + ex);
                return;
            }
            float customHours;
            if (!AnimalRespawnTimeModMain.TryGetConfiguredHours(prefabName, out customHours))
                return;
            __result = customHours;
        }
    }

    // ============================================================
    // EAF COMPATIBILITY
    //
    // Same principle: CustomSpawnRegion.RemoveFromSpawnRegion internally
    // calls its own private GetNumHoursBetweenRespawns(), which our Postfix
    // also intercepts - so patching that one method is sufficient. EAF
    // owns persistence via its SpawnRegionModDataProxy (m_NumRespawnsPending,
    // m_ElapasedHoursNextRespawnAllowed), which is correct now that
    // SpawnModDataProxy.Disconnected is no longer excluded from
    // serialization (upstream EAF fix).
    // ============================================================
    internal static class EAFCompat
    {
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
                DebugHelper.Log("[AnimalRespawnTimeMod] EAF detected. Patching EAF cooldown calculation.");

                MethodInfo getRespawnHoursMethod = AccessTools.Method(customSpawnRegionType, "GetNumHoursBetweenRespawns");
                if (getRespawnHoursMethod == null)
                {
                    MelonLogger.Error("[AnimalRespawnTimeMod] EAF: GetNumHoursBetweenRespawns not found!");
                    return;
                }
                harmony.Patch(
                    getRespawnHoursMethod,
                    postfix: new HarmonyMethod(typeof(EAFCompat), nameof(GetNumHoursBetweenRespawnsPostfix)));

                IsActive = true;
                MelonLogger.Msg("[AnimalRespawnTimeMod] EAF compatibility installed. EAF owns the cooldown and ModData persistence.");
            }
            catch (Exception ex)
            {
                IsActive = false;
                MelonLogger.Error("[AnimalRespawnTimeMod] Failed to patch EAF: " + ex);
            }
        }

        public static void GetNumHoursBetweenRespawnsPostfix(object __instance, ref float __result)
        {
            if (__instance == null)
                return;
            try
            {
                PropertyInfo vanillaProperty = AccessTools.Property(__instance.GetType(), "VanillaSpawnRegion");
                if (vanillaProperty == null)
                    return;
                SpawnRegion region = vanillaProperty.GetValue(__instance) as SpawnRegion;
                if (region == null)
                    return;
                string prefabName = region.GetSpawnablePrefabName();
                float customHours;
                if (!AnimalRespawnTimeModMain.TryGetConfiguredHours(prefabName, out customHours))
                    return;
                __result = customHours;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[AnimalRespawnTimeMod] [EAF] GetNumHoursBetweenRespawnsPostfix failed: " + ex);
            }
        }
    }
}