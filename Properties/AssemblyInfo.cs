using MelonLoader;
using AnimalRespawnTimeMod;
using System.Reflection;
using System.Runtime.InteropServices;

//This is a C# comment. Comments have no impact on compilation.

//ModName, ModVersion, ModAuthor, and ModNamespace.ModClassInheritingFromMelonMod all need changed.

[assembly: AssemblyTitle("AnimalRespawnTimeMod")]
[assembly: AssemblyCopyright("BaltaZar")]

//Version numbers in C# are a set of 1 to 4 positive integers separated by periods.
//Mods typically use 3 numbers. For example: 1.2.1
//The mod version need specified in three places.
<<<<<<< HEAD
[assembly: AssemblyVersion("1.3.0")]
[assembly: AssemblyFileVersion("1.3.0")]
[assembly: MelonInfo(typeof(AnimalRespawnTimeModMain), "AnimalRespawnTimeMod", "1.3.0", "BaltaZar", null)]
=======
[assembly: AssemblyVersion("1.2.0")]
[assembly: AssemblyFileVersion("1.2.0")]
[assembly: MelonInfo(typeof(AnimalRespawnTimeModMain), "AnimalRespawnTimeMod", "1.2.0", "BaltaZar", null)]
>>>>>>> 67f0782db190d6a4e40a95b061d700868d21cfc3

//This tells MelonLoader that the mod is only for The Long Dark.
[assembly: MelonGame("Hinterland", "TheLongDark")]