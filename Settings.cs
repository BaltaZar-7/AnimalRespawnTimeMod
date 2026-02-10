#nullable disable
using MelonLoader;
using ModSettings;

namespace AnimalRespawnTimeMod
{
    internal class AnimalRespawnTimeSettings : JsonModSettings
    {
        public static AnimalRespawnTimeSettings Instance { get; private set; }

        [Section("Respawn Times in DAYS")]
        [Name("Wolf")]
        [Description("Number of days for each Wolf to respawn")]
        [Slider(1, 200)]
        public float Wolfnumber = 20;

        [Name("Bear")]
        [Description("Number of days for each Bear to respawn")]
        [Slider(1, 200)]
        public float Bearnumber = 60;

        [Name("Rabbit")]
        [Description("Number of days for each Rabbit to respawn")]
        [Slider(1, 200)]
        public float Rabbitnumber = 14;

        [Name("Doe")]
        [Description("Number of days for each Doe to respawn")]
        [Slider(1, 200)]
        public float Doenumber = 40;

        [Name("Stag")]
        [Description("Number of days for each Stag to respawn")]
        [Slider(1, 200)]
        public float Stagnumber = 40;

        [Name("Moose")]
        [Description("Number of days for each Moose to respawn")]
        [Slider(1, 200)]
        public float Moosenumber = 80;

        [Name("Ptarmigan")]
        [Description("Number of days for each Ptarmigan to respawn")]
        [Slider(1, 200)]
        public float Ptarmigannumber = 14;

        [Name("Wolf_grey")]
        [Description("Number of days for each Timberwolf to respawn")]
        [Slider(1, 200)]
        public float Wolf_greynumber = 20;

        [Name("Wolf_Starving")]
        [Description("Number of days for each Starving Wolf to respawn")]
        [Slider(1, 200)]
        public float Wolf_Starvingnumber = 20;

        protected override void OnConfirm()
        {
            base.OnConfirm();
            Save();
        }

        internal static void OnLoad()
        {
            Instance = new AnimalRespawnTimeSettings();
            Instance.AddToModSettings("Animal Respawn Time Mod");
        }
    }
}
