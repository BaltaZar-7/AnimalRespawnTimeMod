#nullable disable
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AnimalRespawnTimeMod
{
    public class AnimalRespawnTimeModMain : MelonMod
    {
        public static Dictionary<string, float> RespawnConfig =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("AnimalRespawnTimeTweaks initializing...");
            LoadConfig();
            MelonLogger.Msg("AnimalRespawnTimeTweaks ready");
        }

        private void LoadConfig()
        {
            try
            {
                string cfgDir = Path.Combine(MelonEnvironment.GameRootDirectory, "Mods");
                Directory.CreateDirectory(cfgDir);
                string cfgPath = Path.Combine(cfgDir, "AnimalRespawnTimeTweaks.json");

                if (!File.Exists(cfgPath))
                {
                    Dictionary<string, float> example =
                        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "WILDLIFE_Wolf", 768f },
                            { "WILDLIFE_Bear", 1920f },
                            { "WILDLIFE_Rabbit", 960f },
                            { "WILDLIFE_Doe", 768f },
                            { "WILDLIFE_Stag", 768f },
                            { "WILDLIFE_Moose", 2880f },
                            { "WILDLIFE_Ptarmigan", 640f },
                            { "WILDLIFE_Wolf_grey", 768f },
                            { "WILDLIFE_Wolf_Starving", 768f }
                        };

                    File.WriteAllText(cfgPath,
                        JsonConvert.SerializeObject(example, Formatting.Indented));

                    RespawnConfig = example;
                    MelonLogger.Msg($"[Config] Created example config at {cfgPath}");
                    return;
                }

                string text = File.ReadAllText(cfgPath);
                Dictionary<string, float> loaded =
                    JsonConvert.DeserializeObject<Dictionary<string, float>>(text);

                RespawnConfig = loaded ??
                    new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

                MelonLogger.Msg($"[Config] Loaded {RespawnConfig.Count} entries");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Config] Failed to load config: {ex}");
            }
        }

        public static bool TryGetConfiguredHours(string prefabName, out float hours)
        {
            hours = 0f;

            if (string.IsNullOrEmpty(prefabName))
                return false;

            if (prefabName.EndsWith("(Clone)", StringComparison.OrdinalIgnoreCase))
            {
                prefabName = prefabName.Substring(
                    0,
                    prefabName.Length - "(Clone)".Length
                ).Trim();
            }

            return RespawnConfig.TryGetValue(prefabName, out hours);
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.GetNumHoursBetweenRespawns))]
    public static class SpawnRegion_GetNumHoursBetweenRespawns_Patch
    {
        static void Postfix(SpawnRegion __instance, ref float __result)
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

            float custom;
            if (!AnimalRespawnTimeModMain.TryGetConfiguredHours(prefabName, out custom))
                return;

            __result = custom;

#if DEBUG
            MelonLogger.Msg($"[Respawn] Override GetNumHoursBetweenRespawns {prefabName} -> {custom}h");
#endif
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.SetRespawnCooldownTimer))]
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

            float custom;
            if (!AnimalRespawnTimeModMain.TryGetConfiguredHours(prefabName, out custom))
                return;

            cooldownHours = custom;
            __instance.m_CooldownTimerHours = custom;

#if DEBUG
            MelonLogger.Msg($"[Respawn] Override cooldown {prefabName} -> {custom}h");
#endif
        }
    }
}