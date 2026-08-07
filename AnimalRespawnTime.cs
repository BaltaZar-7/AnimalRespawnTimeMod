#nullable disable
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System;

namespace AnimalRespawnTimeMod
{
    public class AnimalRespawnTimeModMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("AnimalRespawnTimeMod initializing...");
            AnimalRespawnTimeSettings.OnLoad();
            MelonLogger.Msg("AnimalRespawnTimeMod ready");
        }

        public static bool TryGetConfiguredHours(string prefabName, out float hours)
        {
            hours = 0f;

            if (string.IsNullOrEmpty(prefabName))
                return false;

            if (prefabName.EndsWith("(Clone)", StringComparison.OrdinalIgnoreCase))
            {
                prefabName = prefabName.Substring(0,prefabName.Length - "(Clone)".Length).Trim();
            }

            AnimalRespawnTimeSettings s = AnimalRespawnTimeSettings.Instance;
            if (s == null)
                return false;

            float days;

            switch (prefabName)
            {
                case "WILDLIFE_Wolf": days = s.Wolfnumber; break;
                case "WILDLIFE_Bear": days = s.Bearnumber; break;
                case "WILDLIFE_Rabbit": days = s.Rabbitnumber; break;
                case "WILDLIFE_Doe": days = s.Doenumber; break;
                case "WILDLIFE_Stag": days = s.Stagnumber; break;
                case "WILDLIFE_Moose": days = s.Moosenumber; break;
                case "WILDLIFE_Ptarmigan": days = s.Ptarmigannumber; break;
                case "WILDLIFE_Wolf_grey": days = s.Wolf_greynumber; break;
                case "WILDLIFE_Wolf_Starving": days = s.Wolf_Starvingnumber; break;
                default:
                    return false;
            }

            hours = days * 24f;
            return true;
        }
    }
    // With EAF installed, this is needed too to work
    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.Start))]
    public static class SpawnRegion_Start_Patch
    {
        public static void Postfix(SpawnRegion __instance)
        {
            if (__instance == null)
                return;

            string prefab = __instance.GetSpawnablePrefabName();

            float customHours;
            if (!AnimalRespawnTimeModMain.TryGetConfiguredHours(prefab, out customHours))
                return;

            float vanillaDays = __instance.m_NumHoursBetweenRespawns / 24f;

            __instance.m_NumHoursBetweenRespawns = customHours;

            /*MelonLogger.Msg(
                $"[Respawn] {prefab}: {vanillaDays:0.##} -> {customHours / 24f:0.##} days"
            );*/
        }
    }

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
                MelonLogger.Error($"[Respawn] Failed to get prefab name: {ex}");
                return;
            }

            float customHours;
            if (!AnimalRespawnTimeModMain.TryGetConfiguredHours(prefabName, out customHours))
                return;


            float vanillaHours = __result;
            float vanillaDays = vanillaHours / 24f;
            float customDays = customHours / 24f;

            /*MelonLogger.Msg(
                $"[Respawn] {prefabName} vanilla={vanillaDays:0.##} days -> mod={customDays:0.##} days"
            );*/

            __result = customHours;
        }
    }

    /**[HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.SetRespawnCooldownTimer))]
    public static class SpawnRegion_SetRespawnCooldownTimer_Patch
    {
        static void Prefix(SpawnRegion __instance, ref float cooldownHours)
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
                MelonLogger.Error($"[Respawn] Failed to get prefab name: {ex}");
                return;
            }

            float customHours;
            if (!AnimalRespawnTimeModMain.TryGetConfiguredHours(prefabName, out customHours))
                return;

            float vanillaDays = cooldownHours / 24f;
            float customDays = customHours / 24f;

            MelonLogger.Msg(
                $"[Respawn] cooldown {prefabName}: {vanillaDays:0.##} days -> {customDays:0.##} days"
            );

            cooldownHours = customHours;
            __instance.m_CooldownTimerHours = customHours;
        }
    }**/
}