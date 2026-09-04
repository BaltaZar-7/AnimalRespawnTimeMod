#nullable disable
using MelonLoader;
using MelonLoader.Utils;
using System.IO;

namespace AnimalRespawnTimeMod
{
    internal static class DebugHelper
    {
        private static bool _debugEnabled = false;
        private static readonly string DebugFile = Path.Combine(MelonEnvironment.UserDataDirectory, "animalrespawntime.debug");

        internal static void Init()
        {
            _debugEnabled = File.Exists(DebugFile);

            if (_debugEnabled)
                MelonLogger.Msg("[AnimalRespawnTimeMod] Debug ENABLED");
        }

        internal static void Log(string msg)
        {
            if (_debugEnabled)
                MelonLogger.Msg("[AnimalRespawnTimeMod DEBUG] " + msg);
        }
    }
}