using HarmonyLib;

namespace AlbaVR
{
    [HarmonyPatch]
    public class CharacterMeshesPatcher
    {
        public static bool VisibilityChangeIsInternal { get; set; }
        public static bool DesiredVisibility { get; private set; }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterMeshes), "SetVisible")]
        private static void StoreDesiredVisibility(bool active)
        {
            if (!VisibilityChangeIsInternal)
            {
                DesiredVisibility = active;
            }
        }
    }
}
