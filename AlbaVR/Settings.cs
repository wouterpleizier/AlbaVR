using BepInEx.Configuration;
using UnityEngine;

namespace AlbaVR
{
    public static class Settings
    {
        private const string _fullFirstPersonModeCategory = "AlbaVR.FirstPersonMode";
        public static ConfigEntry<bool> EnableFullFirstPersonMode { get; private set; }
        public static ConfigEntry<bool> EnableSnapTurn { get; private set; }
        public static ConfigEntry<float> SnapTurnIncrement { get; private set; }
        public static ConfigEntry<bool> AllowSnapTurnInInventory { get; private set; }
        public static ConfigEntry<float> SmoothTurnSpeed { get; private set; }
        public static ConfigEntry<float> TurnInputDeadZone { get; private set; }

        private const string _generalCategory = "AlbaVR.General";
        public static ConfigEntry<bool> ShowHUD { get; private set; }
        public static ConfigEntry<float> ThirdPersonCameraHeight { get; private set; }

        private const string _hotkeysCategory = "AlbaVR.Hotkeys";
        public static ConfigEntry<KeyCode> RecenterKey { get; private set; }
        public static ConfigEntry<KeyCode> ToggleHUDKey { get; private set; }
        public static ConfigEntry<KeyCode> ToggleFullFirstPersonModeKey { get; private set; }

        private const string _vrQualitySettingsCategory = "AlbaVR.QualitySettings";
        public static ConfigEntry<float> LodBiasMultiplier { get; private set; }

        private const string _debugSettingsCategory = "Debug";
        public static ConfigEntry<bool> ForceInitializeVRMod { get; private set; }
        public static ConfigEntry<bool> EnableTimeScaleControl { get; private set; }

        public static void Initialize(string configPath)
        {
            ConfigFile configFile = new ConfigFile(configPath, true);

            // Full first person mode

            EnableFullFirstPersonMode = configFile.Bind(
                _fullFirstPersonModeCategory, nameof(EnableFullFirstPersonMode), false,
                new ConfigDescription(
                    "Normally, the game only uses a first-person perspective when using inventory\n" +
                    "items. Enabling this option forces the game to also use this perspective when\n" +
                    "walking around, along with some comfort adjustments. Does not affect dialogs,\n" +
                    "cutscenes and certain interactions. Can be toggled in-game using the key\n" +
                    "defined in ToggleFullFirstPersonModeKey (Insert by default).",
                    new AcceptableValueList<bool>(false, true)));

            EnableSnapTurn = configFile.Bind(
                _fullFirstPersonModeCategory, nameof(EnableSnapTurn), true,
                new ConfigDescription(
                    "True for snap turn, false for smooth turn. Only used in full first person\n" +
                    "mode and when gamepad controls are used.",
                    new AcceptableValueList<bool>(false, true)));

            SnapTurnIncrement = configFile.Bind(
                _fullFirstPersonModeCategory, nameof(SnapTurnIncrement), 30f,
                new ConfigDescription(
                    "The turn angle in degrees when using snap turn.",
                    new AcceptableValueRange<float>(10f, 90f)));

            AllowSnapTurnInInventory = configFile.Bind(
                _fullFirstPersonModeCategory, nameof(AllowSnapTurnInInventory), false,
                new ConfigDescription(
                    "It's important that the in-game camera can be aimed freely even when snap\n" +
                    "turn is enabled, as animals can be difficult to photograph otherwise. If this\n" +
                    "is uncomfortable, enable this option to decouple the VR view's rotation from\n" +
                    "the camera aim. The camera can then still be aimed freely, and the VR view\n" +
                    "will snap to the nearest angle corresponding to SnapTurnIncrement.",
                    new AcceptableValueList<bool>(false, true)));

            SmoothTurnSpeed = configFile.Bind(
                _fullFirstPersonModeCategory, nameof(SmoothTurnSpeed), 75f,
                new ConfigDescription(
                    "The turn angle in degrees per second when using smooth turning.",
                    new AcceptableValueRange<float>(10f, 200f)));

            TurnInputDeadZone = configFile.Bind(
                _fullFirstPersonModeCategory, nameof(TurnInputDeadZone), 0.25f,
                new ConfigDescription(
                    "How far the analog stick or mouse has to be pushed to trigger a turn. For\n" +
                    "gamepad, a value of 0 means the analog stick is centered and 1 means that\n" +
                    "it's fully pushed to the left or right. For mouse, the value equals the\n" +
                    "movement distance in pixels during a single frame.",
                    new AcceptableValueRange<float>(0.01f, 100f)));

            // General

            ShowHUD = configFile.Bind(
                _generalCategory, nameof(ShowHUD), true,
                new ConfigDescription(
                    "Determines whether the HUD should be rendered. Does not affect interaction\n" +
                    "markers in the world.",
                    new AcceptableValueList<bool>(true, false)));

            ThirdPersonCameraHeight = configFile.Bind(
                _generalCategory, nameof(ThirdPersonCameraHeight), 1.5f,
                new ConfigDescription(
                    "Camera height offset (measured in meters) when playing in third person mode.",
                    new AcceptableValueRange<float>(0f, 3f)));

            // Hotkeys

            RecenterKey = configFile.Bind(
                _hotkeysCategory, nameof(RecenterKey), KeyCode.Home,
                new ConfigDescription(
                    "The key that makes the VR origin/camera realign to your current position.\n" +
                    "Does not affect rotation, so prefer to use the built-in recentering function\n" +
                    "of your HMD/VR software when possible.",
                    new AcceptableKeyCodeValueList()));

            ToggleHUDKey = configFile.Bind(
                _hotkeysCategory, nameof(ToggleHUDKey), KeyCode.H,
                new ConfigDescription(
                    "The key that toggles HUD rendering. Does not affect interaction markers in\n" +
                    "the world.",
                    new AcceptableKeyCodeValueList()));

            ToggleFullFirstPersonModeKey = configFile.Bind(
                _hotkeysCategory, nameof(ToggleFullFirstPersonModeKey), KeyCode.Insert,
                new ConfigDescription(
                    "The key that toggles the full first person mode.",
                    new AcceptableKeyCodeValueList()));

            // Quality settings

            LodBiasMultiplier = configFile.Bind(
                _vrQualitySettingsCategory, nameof(LodBiasMultiplier), 0f,
                new ConfigDescription(
                    "Increases or decreases the distance at which objects reduce their detail or\n" +
                    "stop rendering. By default, the higher FOV in VR causes this distance to be\n" +
                    "much shorter. This multiplier is applied on top of the in-game LOD setting,\n" +
                    "allowing it to match the non-VR distances more closely. A value of around 2.4\n" +
                    "is recommended for Reverb/Quest, or around 3.0 for Vive/Index. Set to 0 to\n" +
                    "calculate the appropriate value automatically based on your headset FOV.",
                    new AcceptableValueRange<float>(0f, 10f)));

            // Debug

            ForceInitializeVRMod = configFile.Bind(
                _debugSettingsCategory, nameof(ForceInitializeVRMod), false,
                new ConfigDescription(
                    "Forces the mod to initialize even when no HMD is active/detected. Intended\n" +
                    "for development/debugging only.",
                    new AcceptableValueList<bool>(false, true)));

            EnableTimeScaleControl = configFile.Bind(
                _debugSettingsCategory, nameof(EnableTimeScaleControl), false,
                new ConfigDescription(
                    "Allows increasing and decreasing the time scale using the plus and minus\n" +
                    "keys. Intended for development/debugging only.",
                    new AcceptableValueList<bool>(false, true)));
        }
    }
}
