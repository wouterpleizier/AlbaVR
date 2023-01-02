using Cinemachine;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlbaVR
{
    public class VRFirstPersonModeController : MonoBehaviour
    {
        // For clarity's sake: 'native first person' refers to the first person mode that's already in the game when
        // using inventory items like the phone camera. 'Full first person' refers to our custom mode that also enables
        // a first person view when walking around.

        private Camera _vrCamera;

        private CinemachineVirtualCamera _nativeFirstPersonVirtualCamera;
        private CinemachinePOV _nativeFirstPersonVirtualCameraAimComponent;

        private CinemachineFreeLook _thirdPersonVirtualCamera;
        private CinemachineCollider _thirdPersonVirtualCameraCollider;
        private float _thirdPersonVirtualCameraXAxisMaxSpeed;
        private float _thirdPersonVirtualCameraYAxisMaxSpeed;
        private Dictionary<CinemachineOrbitalTransposer, float> _thirdPersonVirtualCameraTransposerDampingValues;

        private CharacterMeshes _characterMeshes;
        private PlayerAnimationProvider _playerAnimationProvider;
        private SkinnedMeshRenderer _bodyMeshRenderer;

        private SnapPositionToFirstPersonCamera _snapPositionToFirstPersonCameraComponent;
        private float _handMeshesForwardOffset = 0.25f;
        private Vector3 _handMeshesSmoothDampVelocity;
        private float _handMeshesSmoothDampDuration = 0.25f;

        private float _cameraLookXInput;
        private bool _turnInputHasRecentered;

        private bool _isInNativeFirstPersonMode;
        private float _nativeFirstPersonModeSnapTurnYaw;

        public Vector3 FirstPersonCameraPosition { get; private set; }
        public Quaternion FirstPersonCameraRotation { get; private set; }

        public bool FullFirstPersonModeIsActive
        {
            get
            {
                CinemachineVirtualCameraBase activeVirtualCamera = CinemachineController.Instance.brain.ActiveVirtualCamera as CinemachineVirtualCameraBase;

                return Settings.EnableFullFirstPersonMode.Value == true
                    && (activeVirtualCamera == _nativeFirstPersonVirtualCamera || activeVirtualCamera == _thirdPersonVirtualCamera);
            }
        }

        public bool SnapTurnIsActive
        {
            get
            {
                return FullFirstPersonModeIsActive && Settings.EnableSnapTurn.Value && LPN.InputWatcher.IsUsingGamepad;
            }
        }

        private void Start()
        {
            _vrCamera = GetComponentInChildren<Camera>();

            _nativeFirstPersonVirtualCamera = (CinemachineVirtualCamera)CinemachineController.Instance.cameras.firstPersonCamera;
            _nativeFirstPersonVirtualCameraAimComponent = (CinemachinePOV)_nativeFirstPersonVirtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Aim);

            _thirdPersonVirtualCamera = CinemachineController.Instance.cameras.GetFreeLook();
            _thirdPersonVirtualCameraCollider = _thirdPersonVirtualCamera.gameObject.GetComponent<CinemachineCollider>();
            _thirdPersonVirtualCameraXAxisMaxSpeed = _thirdPersonVirtualCamera.m_XAxis.m_MaxSpeed;
            _thirdPersonVirtualCameraYAxisMaxSpeed = _thirdPersonVirtualCamera.m_YAxis.m_MaxSpeed;

            _thirdPersonVirtualCameraTransposerDampingValues = new Dictionary<CinemachineOrbitalTransposer, float>();
            foreach (CinemachineOrbitalTransposer transposer in _thirdPersonVirtualCamera.GetComponentsInChildren<CinemachineOrbitalTransposer>())
            {
                _thirdPersonVirtualCameraTransposerDampingValues.Add(transposer, transposer.m_XDamping);
            }

            _characterMeshes = Service<CharacterMeshes>.Instance;
            _playerAnimationProvider = _characterMeshes.GetComponent<PlayerAnimationProvider>();

            // This event is fired when the player model changes, e.g. from baby Alba to child Alba after the prologue.
            _playerAnimationProvider.OnAnimationInterfaceChanged += CacheBodyMeshRenderer;
            CacheBodyMeshRenderer();

            _snapPositionToFirstPersonCameraComponent = FindObjectOfType<SnapPositionToFirstPersonCamera>();

            NQInputManager.RegisterGameHookForAction<Vector2>(NQActionData.ThirdPersonCameraLook, new GameInputCallback<Vector2>(HandleCameraLook));
            NQInputManager.RegisterGameHookForAction<Vector2>(NQActionData.LookAroundFirstPerson, new GameInputCallback<Vector2>(HandleCameraLook));
            UnityEngine.InputSystem.InputSystem.onAfterUpdate += HandleInputSystemAfterUpdate;

            CinemachineControllerPatcher.CameraMotionSettingsUpdated += FixCameraMotionSettings;
            FixCameraMotionSettings();

            Service<InventoryService>.Instance.OnCurrentInventoryItemChangedFromTo += HandleCurrentInventoryItemChanged;
            Singleton<GameModeSwitcher>.Instance.SwitchedToFirstPerson += HandleSwitchedToNativeFirstPersonMode;
            Singleton<GameModeSwitcher>.Instance.SwitchedToThirdPerson += HandleSwitchedFromNativeFirstPersonMode;
        }

        private void HandleCurrentInventoryItemChanged(InventoryDataItem arg1, InventoryDataItem arg2)
        {
            // When switching from one inventory item to another, the native first person camera's pitch remains the
            // same. This doesn't feel right in VR when switching to an item that doesn't let you control the pitch.
            ResetNativeFirstPersonCameraPitch();
        }

        private void HandleSwitchedToNativeFirstPersonMode()
        {
            // The initial aim direction of the native first person camera is copied from the third person camera or the
            // This doesn't feel intuitive in VR, especially when you're playing the entire game in first person mode
            // and you're controlling the camera pitch without noticing it. We'll set the vertical input axis to this
            // value so that the initial aim is aligned to the horizon.
            ResetNativeFirstPersonCameraPitch();

            _nativeFirstPersonModeSnapTurnYaw = CinemachineController.Instance.GetOutputCamera().transform.rotation.eulerAngles.y;
            _isInNativeFirstPersonMode = true;
        }

        private void HandleSwitchedFromNativeFirstPersonMode()
        {
            _isInNativeFirstPersonMode = false;
        }

        private void ResetNativeFirstPersonCameraPitch()
        {
            _nativeFirstPersonVirtualCameraAimComponent.m_VerticalAxis.Value = 0.5f;
        }

        private void CacheBodyMeshRenderer()
        {
            SkinnedMeshRenderer result = null;
            if (_playerAnimationProvider.GetAnimationInterface() is MonoBehaviour characterAnimationBehaviour)
            {
                foreach (SkinnedMeshRenderer renderer in characterAnimationBehaviour.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer.gameObject.name == "body_mesh")
                    {
                        result = renderer;
                        break;
                    }
                }
            }

            _bodyMeshRenderer = result;
        }

        private void OnDestroy()
        {
            _playerAnimationProvider.OnAnimationInterfaceChanged -= CacheBodyMeshRenderer;

            NQInputManager.UnRegisterGameHookForAction<Vector2>(NQActionData.ThirdPersonCameraLook, new GameInputCallback<Vector2>(HandleCameraLook));
            NQInputManager.UnRegisterGameHookForAction<Vector2>(NQActionData.LookAroundFirstPerson, new GameInputCallback<Vector2>(HandleCameraLook));
            UnityEngine.InputSystem.InputSystem.onAfterUpdate -= HandleInputSystemAfterUpdate;

            CinemachineControllerPatcher.CameraMotionSettingsUpdated -= FixCameraMotionSettings;

            Singleton<GameModeSwitcher>.Instance.SwitchedToFirstPerson -= HandleSwitchedToNativeFirstPersonMode;
            Singleton<GameModeSwitcher>.Instance.SwitchedToThirdPerson -= HandleSwitchedFromNativeFirstPersonMode;
        }

        public void ToggleFullFirstPersonMode()
        {
            Settings.EnableFullFirstPersonMode.Value = !Settings.EnableFullFirstPersonMode.Value;
            FixCameraMotionSettings();
        }

        private void FixCameraMotionSettings()
        {
            if (Settings.EnableFullFirstPersonMode.Value == true)
            {
                _thirdPersonVirtualCameraCollider.enabled = false;
                _thirdPersonVirtualCamera.m_XAxis.m_MaxSpeed = 0f;
                _thirdPersonVirtualCamera.m_YAxis.m_MaxSpeed = 0f;
                _thirdPersonVirtualCamera.m_YAxis.Value = 0.5f;

                foreach (CinemachineOrbitalTransposer transposer in _thirdPersonVirtualCameraTransposerDampingValues.Keys)
                {
                    transposer.m_XDamping = 0f;
                }
            }
            else
            {
                _thirdPersonVirtualCameraCollider.enabled = true;
                _thirdPersonVirtualCamera.m_XAxis.m_MaxSpeed = _thirdPersonVirtualCameraXAxisMaxSpeed;
                _thirdPersonVirtualCamera.m_YAxis.m_MaxSpeed = _thirdPersonVirtualCameraYAxisMaxSpeed;

                foreach (KeyValuePair<CinemachineOrbitalTransposer, float> keyValuePair in _thirdPersonVirtualCameraTransposerDampingValues)
                {
                    CinemachineOrbitalTransposer transposer = keyValuePair.Key;
                    float originalXDamping = keyValuePair.Value;

                    transposer.m_XDamping = originalXDamping;
                }
            }

            _snapPositionToFirstPersonCameraComponent.enabled = false;
        }

        private void LateUpdate()
        {
            if (Input.GetKeyDown(Settings.ToggleFullFirstPersonModeKey.Value))
            {
                ToggleFullFirstPersonMode();
            }

            UpdateCameraTransform();
            UpdateLookInput();
            UpdateCharacterMeshesVisibility();
            UpdateFirstPersonHandsPosition();
        }

        private void UpdateCameraTransform()
        {
            if (FullFirstPersonModeIsActive)
            {
                _nativeFirstPersonVirtualCamera.UpdateCameraState(Vector3.up, Time.deltaTime);
                FirstPersonCameraPosition = _nativeFirstPersonVirtualCamera.transform.position;

                float yaw = CinemachineController.Instance.GetOutputCamera().transform.rotation.eulerAngles.y;
                if (_isInNativeFirstPersonMode && SnapTurnIsActive && Settings.AllowSnapTurnInInventory.Value == true)
                {
                    float difference = Mathf.DeltaAngle(_nativeFirstPersonModeSnapTurnYaw, yaw);
                    if (Mathf.Abs(difference) > Settings.SnapTurnIncrement.Value)
                    {
                        _nativeFirstPersonModeSnapTurnYaw += Settings.SnapTurnIncrement.Value * Mathf.Sign(difference);

                        // Modulo taking negative values into account, to ensure we're staying in the 0-360 range.
                        _nativeFirstPersonModeSnapTurnYaw -= 360f * Mathf.Floor(_nativeFirstPersonModeSnapTurnYaw / 360f);
                    }

                    yaw = _nativeFirstPersonModeSnapTurnYaw;
                }

                FirstPersonCameraRotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        private void UpdateCharacterMeshesVisibility()
        {
            if (_bodyMeshRenderer != null)
            {
                bool areCharacterMeshesVisible = _bodyMeshRenderer.enabled;
                if (FullFirstPersonModeIsActive && areCharacterMeshesVisible)
                {
                    CharacterMeshesPatcher.VisibilityChangeIsInternal = true;
                    _characterMeshes.SetVisible(false);
                    CharacterMeshesPatcher.VisibilityChangeIsInternal = false;
                }
                else if (!FullFirstPersonModeIsActive && CharacterMeshesPatcher.DesiredVisibility != areCharacterMeshesVisible)
                {
                    CharacterMeshesPatcher.VisibilityChangeIsInternal = true;
                    _characterMeshes.SetVisible(CharacterMeshesPatcher.DesiredVisibility);
                    CharacterMeshesPatcher.VisibilityChangeIsInternal = false;
                }
            }
        }

        private void UpdateFirstPersonHandsPosition()
        {
            Vector3 handsTargetPosition = _vrCamera.transform.position
                + (Vector3.up * _snapPositionToFirstPersonCameraComponent.yPositionOffset)
                + (_vrCamera.transform.forward * _handMeshesForwardOffset);

            _snapPositionToFirstPersonCameraComponent.transform.position = Vector3.SmoothDamp(
                _snapPositionToFirstPersonCameraComponent.transform.position,
                handsTargetPosition,
                ref _handMeshesSmoothDampVelocity,
                _handMeshesSmoothDampDuration);
        }

        private void UpdateLookInput()
        {
            if (FullFirstPersonModeIsActive && !FirstPersonOrienter.ForbidMovement)
            {
                if (SnapTurnIsActive)
                {
                    if (Mathf.Abs(_cameraLookXInput) > Settings.TurnInputDeadZone.Value)
                    {
                        if (_turnInputHasRecentered)
                        {
                            _thirdPersonVirtualCamera.m_XAxis.Value += Settings.SnapTurnIncrement.Value * -Mathf.Sign(_cameraLookXInput);
                            _turnInputHasRecentered = false;
                        }
                    }
                    else
                    {
                        _turnInputHasRecentered = true;
                    }
                }
                else
                {
                    if (Mathf.Abs(_cameraLookXInput) > Settings.TurnInputDeadZone.Value)
                    {
                        _thirdPersonVirtualCamera.m_XAxis.Value -= Mathf.Sign(_cameraLookXInput) * Settings.SmoothTurnSpeed.Value * Time.deltaTime;
                    }
                }
            }
        }

        private void HandleCameraLook(Vector2 input)
        {
            _cameraLookXInput = input.x;
        }

        private void HandleInputSystemAfterUpdate()
        {
            _cameraLookXInput = 0f;
        }
    }
}
