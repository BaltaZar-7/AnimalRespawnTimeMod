#nullable disable
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace AnimalRespawnTimeMod
{
    public class AnimalRespawnTimeModMain : MelonMod
    {
        public static readonly Dictionary<string, float> Values = new Dictionary<string, float>()
        {
            { "WILDLIFE_Wolf", 1f },
            { "WILDLIFE_Bear", 1f },
            { "WILDLIFE_Rabbit", 1f },
            { "WILDLIFE_Doe", 1f },
            { "WILDLIFE_Stag", 1f },
            { "WILDLIFE_Moose", 1f },
            { "WILDLIFE_Ptarmigan", 1f },
            { "WILDLIFE_Wolf_grey", 1f },
            { "WILDLIFE_Wolf_Starving", 1f }
        };

        public static Dictionary<string, float> RespawnConfig =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public static bool Debug_ForceRespawnAllowed = false;

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

                    File.WriteAllText(
                        cfgPath,
                        JsonConvert.SerializeObject(example, Formatting.Indented)
                    );

                    RespawnConfig = example;
                    MelonLogger.Msg($"[Config] Created example config at {cfgPath}");
                }
                else
                {
                    string text = File.ReadAllText(cfgPath);

                    Dictionary<string, float> loaded =
                        JsonConvert.DeserializeObject<Dictionary<string, float>>(text);

                    RespawnConfig = loaded
                        ?? new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

                    MelonLogger.Msg(
                        $"[Config] Loaded {RespawnConfig.Count} respawn entries from {cfgPath}"
                    );

                    foreach (KeyValuePair<string, float> kv in RespawnConfig)
                    {
                        MelonLogger.Msg($"[Config] {kv.Key} -> {kv.Value}h");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Config] Failed to load config: {ex}");
            }
        }

        public static bool TryGetConfiguredHoursForInstance(
            string instanceNameOrPrefab,
            out float hours
        )
        {
            hours = 0f;
            if (string.IsNullOrEmpty(instanceNameOrPrefab))
                return false;

            if (RespawnConfig != null &&
                RespawnConfig.TryGetValue(instanceNameOrPrefab, out hours))
                return true;

            string name = instanceNameOrPrefab;

            if (name.EndsWith("(Clone)", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - "(Clone)".Length).Trim();
            }

            if (RespawnConfig.TryGetValue(name, out hours))
                return true;

            foreach (KeyValuePair<string, float> kv in RespawnConfig)
            {
                if (kv.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    hours = kv.Value;
                    return true;
                }
            }

            try
            {
                MethodInfo method =
                    typeof(SpawnRegion).GetMethod(
                        "GetPrefabNameFromInstanceName",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                    );

                if (method != null)
                {
                    string prefab =
                        method.Invoke(null, new object[] { instanceNameOrPrefab }) as string;

                    if (!string.IsNullOrEmpty(prefab) &&
                        RespawnConfig.TryGetValue(prefab, out hours))
                        return true;

                    if (!string.IsNullOrEmpty(prefab))
                    {
                        foreach (KeyValuePair<string, float> kv in RespawnConfig)
                        {
                            if (kv.Key.Equals(prefab, StringComparison.OrdinalIgnoreCase))
                            {
                                hours = kv.Value;
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public static object TryGetPrivateField(object instance, string fieldName)
        {
            try
            {
                if (instance == null || string.IsNullOrEmpty(fieldName))
                    return null;

                FieldInfo fi =
                    instance.GetType().GetField(
                        fieldName,
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                    );

                return fi != null ? fi.GetValue(instance) : null;
            }
            catch
            {
                return null;
            }
        }

        public static bool TrySetPrivateField(object instance, string fieldName, object value)
        {
            try
            {
                if (instance == null || string.IsNullOrEmpty(fieldName))
                    return false;

                FieldInfo fi =
                    instance.GetType().GetField(
                        fieldName,
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                    );

                if (fi == null)
                    return false;

                fi.SetValue(instance, value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), "GetNumHoursBetweenRespawns")]
    public static class SpawnRegion_GetNumHoursBetweenRespawns_Patch
    {
        static void Postfix(SpawnRegion __instance, ref float __result)
        {
            try
            {
                string prefab = "<unknown>";
                try
                {
                    prefab = __instance.GetSpawnablePrefabName();
                }
                catch
                {
                }

                float custom;
                if (AnimalRespawnTimeModMain.TryGetConfiguredHoursForInstance(prefab, out custom))
                {
                    __result = custom;
#if DEBUG
                    MelonLogger.Msg(
                        $"[DEBUG] Overriding GetNumHoursBetweenRespawns for '{prefab}': {custom}h"
                    );
#endif
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[DEBUG] GetNumHoursBetweenRespawns postfix error: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), "SetRespawnCooldownTimer")]
    public static class SpawnRegion_SetRespawnCooldownTimer_Patch
    {
        static void Prefix(SpawnRegion __instance, ref float cooldownHours)
        {
            try
            {
                string prefab = "<unknown>";
                try
                {
                    prefab = __instance.GetSpawnablePrefabName();
                }
                catch
                {
                }

                float custom;
                if (AnimalRespawnTimeModMain.TryGetConfiguredHoursForInstance(prefab, out custom))
                {
#if DEBUG
                    MelonLogger.Msg(
                        $"[DEBUG] Overriding cooldown for '{prefab}' => {custom}h"
                    );
#endif
                    cooldownHours = custom;
                    AnimalRespawnTimeModMain.TrySetPrivateField(
                        __instance,
                        "m_CooldownTimerHours",
                        custom
                    );
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[DEBUG] SetRespawnCooldownTimer prefix error: {ex}");
            }
        }
    }
}