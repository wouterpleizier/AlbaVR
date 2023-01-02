using HarmonyLib;

namespace AlbaVR
{
    [HarmonyPatch]
    public class CinemachineControllerPatcher
    {
        public delegate void CameraMotionSettingsUpdatedEvent();

        public static event CameraMotionSettingsUpdatedEvent CameraMotionSettingsUpdated;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CinemachineController), "UpdateCameraMotionSettings")]
        private static void InvokeCameraMotionSettingsUpdatedEvent()
        {
            CameraMotionSettingsUpdated?.Invoke();
        }
    }
}
