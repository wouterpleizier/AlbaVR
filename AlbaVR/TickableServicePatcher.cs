using HarmonyLib;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace AlbaVR
{
    [HarmonyPatch]
    public class TickableServicePatcher
    {
        public static Camera vrCamera;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TickableService), "ScheduleJob")]
        private static bool OverrideCameraData(TickableService __instance, ref TickableService.CheckDistanceJob ____distanceJob, Transform ____characterTransform, ref JobHandle ____distanceJobHandle, ref TransformAccessArray ____transformAccessArray, ref bool ____jobScheduled)
        {
            if (vrCamera != null)
            {
                // Same as orignal method.
                ____distanceJob.albaPosition = ____characterTransform.position;

                // Passing the VR camera position/matrix here.
                ____distanceJob.cameraPosition = vrCamera.transform.position;
                ____distanceJob.cameraMatrix = vrCamera.worldToCameraMatrix;

                // Same as orignal method.
                ____distanceJobHandle = ____distanceJob.Schedule(____transformAccessArray, default(JobHandle));
                ____jobScheduled = true;
                JobHandle.ScheduleBatchedJobs();

                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
