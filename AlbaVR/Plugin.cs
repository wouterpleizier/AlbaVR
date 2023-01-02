using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace AlbaVR
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo($"Loaded {PluginInfo.PLUGIN_GUID}");

            InitializeSettings();

            if (!IsHMDConnected() && Settings.ForceInitializeVRMod.Value == false)
            {
                Logger.LogWarning("HMD not detected, aborting");
                return;
            }

            Logger.LogInfo("Initializing patches");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            Logger.LogInfo("Waiting for initial loading screen to finish");
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private bool IsHMDConnected()
        {
            List<InputDevice> headDevices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.Head, headDevices);

            return headDevices.Count > 0;
        }

        private void InitializeSettings()
        {
            string configPath = Path.Combine(Paths.GameRootPath, "AlbAVR.ini");

            Logger.LogInfo($"Initializing config at {configPath}");
            Settings.Initialize(configPath);
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            // This scene first gets unloaded when the main menu is ready to use and the necessary game objects exist.
            // We could probably intialize things sooner with a little more work, but this is good enough for now.
            if (scene.name == "LoadingScene")
            {
                Logger.LogInfo("Initializing VR mod");
                InitializeVR();

                SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            }
        }

        private void InitializeVR()
        {
            new GameObject("VRRoot").AddComponent<VRRoot>();
        }
    }
}
