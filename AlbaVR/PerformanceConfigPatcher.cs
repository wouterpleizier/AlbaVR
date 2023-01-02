using HarmonyLib;

namespace AlbaVR
{
    [HarmonyPatch]
    public class PerformanceConfigPatcher
    {
        public delegate void PerformanceConfigAppliedHandler(PerformanceConfig performanceConfig);

        public static event PerformanceConfigAppliedHandler PerformanceConfigApplied;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PerformanceConfig), "ApplyConfiguration")]
        private static void InvokePerformanceConfigAppliedEvent(PerformanceConfig __instance)
        {
            PerformanceConfigApplied?.Invoke(__instance);
        }
    }
}
