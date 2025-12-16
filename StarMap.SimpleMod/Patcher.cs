using HarmonyLib;
using KSA;
using StarMap.SimpleMod.Dependency;

namespace StarMap.SimpleExampleMod
{
    [HarmonyPatch]
    internal static class Patcher
    {
        private static Harmony? _harmony = new Harmony("StarMap.SimpleMod");

        public static void Patch()
        {
            Console.WriteLine("Patching SimpleMod...");
            _harmony?.PatchAll(typeof(Patcher).Assembly);
        }

        public static void Unload()
        {
            _harmony?.UnpatchAll(_harmony.Id);
            _harmony = null;
        }

        [HarmonyPatch(typeof(DependencyClass), nameof(DependencyClass.DoSomething))]
        [HarmonyPrefix]
        public static bool AfterLoad(ref string __result)
        {
            __result = "My custom string";
            return false; // Skip original method entirely
        }
    }
}
