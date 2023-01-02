using Cinemachine;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlbaVR
{
    public class VRRoot : MonoBehaviour
    {
        private Camera _cinemachineCamera;
        private Camera _firstPersonCamera;
        private int _cinemachineCameraCullingMask;
        private int _firstPersonCameraCullingMask;
        private Camera _vrCamera;

        private CinemachineVirtualCameraBase _firstPersonVirtualCamera;
        private CinemachineFreeLook _thirdPersonVirtualCamera;

        private Canvas _uiCanvas;
        private Dictionary<Material, Material> _uiMaterialMappings = new Dictionary<Material, Material>();
        private Canvas _dialogCanvas;
        private Transform _endGameCutscene;
        private Canvas _endGameCanvas;

        private InGameViewController _inGameViewController;
        private CanvasGroup _inGameViewCanvasGroup;
        private Transform _newspaperTransform;
        private VRFirstPersonModeController _vrFirstPersonController;

        private Vector3 _vrPositionOffset;
        private bool _vrCameraHasAlignedToHead;

        private void Start()
        {
            InitializeVRCamera();
            InitializeGameCameras();
            InitializeUI();
            InitializeComponents();

            ConversationDialoguePatcher.EnteredDialog += InitializeDialog;
        }

        private void InitializeComponents()
        {
            gameObject.AddComponent<VRImageCarousel>();

            _vrFirstPersonController = gameObject.AddComponent<VRFirstPersonModeController>();
        }

        private void InitializeVRCamera()
        {
            var vrCameraObject = new GameObject("VRCamera");
            vrCameraObject.transform.parent = transform;

            _vrCamera = vrCameraObject.AddComponent<Camera>();
            _vrCamera.stereoTargetEye = StereoTargetEyeMask.Both;

            vrCameraObject.AddComponent<VRQualitySettings>();

            Singleton<GameModeSwitcher>.Instance.SwitchedToFirstPerson += AlignCameraToHead;

            // Change some camera references that are used for LOD and culling.
            var dynamicObjectLODManager = Service<DynamicObjectLODManager>.Instance;
            typeof(DynamicObjectLODManager).GetField("_mainCamera", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dynamicObjectLODManager, _vrCamera);
            TickableServicePatcher.vrCamera = _vrCamera;

            // At this point, the VR camera still has its default position and rotation. It first gets updated before
            // the cameras start rendering, so we'll use this event to do the initial head alignment.
            Camera.onPreRender += HandleCameraPreRender;
        }

        private void InitializeGameCameras()
        {
            _cinemachineCamera = GameObject.Find("CinemachineCamera").GetComponent<Camera>();
            _firstPersonCamera = GameObject.Find("FirstPersonCamera").GetComponent<Camera>();

            _firstPersonVirtualCamera = CinemachineController.Instance.cameras.firstPersonCamera;
            _thirdPersonVirtualCamera = CinemachineController.Instance.cameras.GetFreeLook();

            foreach (Camera camera in Resources.FindObjectsOfTypeAll<Camera>())
            {
                if (camera != _vrCamera)
                {
                    camera.stereoTargetEye = StereoTargetEyeMask.None;
                }
            }

            // Rendering the non-VR cameras is costly and unnecessary, but disabling them or permanently setting their
            // cullingMask to 0 seems to break some things (camera movement, world-space interaction icons, maybe more).
            // Instead, we'll set the layerMask to 0 during rendering only. Maybe not the best way but it works.
            Camera.onPreCull += HandleCameraPreCull;
            Camera.onPostRender += HandleCameraPostRender;
        }
        
        private void InitializeUI()
        {
            _uiCanvas = FindObjectOfType<CanvasService>().GetComponent<Canvas>();
            _uiCanvas.renderMode = RenderMode.WorldSpace;
            _uiCanvas.transform.parent = transform;
            _uiCanvas.transform.localRotation = Quaternion.identity;
            _uiCanvas.transform.localScale = Vector3.one * 0.001f;
            FixUIMaterials();

            _inGameViewController = FindObjectOfType<InGameViewController>();
            _inGameViewCanvasGroup = _inGameViewController.GetComponent<CanvasGroup>();

            _newspaperTransform = FindObjectOfType<NewspaperContainer>().transform.Find("Animator/NewpaperImage");

            foreach (GameObject rootGameObject in UnityEngine.SceneManagement.SceneManager.GetSceneByName("Layout_Quest_Party").GetRootGameObjects())
            {
                if (rootGameObject.name == "Cutscenes")
                {
                    _endGameCutscene = rootGameObject.transform.Find("LorenaPerformance_cutscene");

                    _endGameCanvas = _endGameCutscene.Find("EndGameCanvas").GetComponent<Canvas>();
                    _endGameCanvas.renderMode = RenderMode.WorldSpace;
                    _endGameCanvas.transform.parent = transform;
                    _endGameCanvas.transform.localRotation = Quaternion.identity;
                    _endGameCanvas.transform.localScale = Vector3.one * 0.001f;
                    break;
                }
            }

            UpdateUIPosition();
        }

        private void FixUIMaterials()
        {
            // Replace UI materials by a copy that renders in front of everything. Adapted from Julien-Lynge's answer
            // here: https://answers.unity.com/questions/878667/world-space-canvas-on-top-of-everything.html

            Shader textMeshProOverlayShader = Shader.Find("TextMeshPro/Distance Field Overlay");
            Shader textMeshProMobileOverlayShader = Shader.Find("TextMeshPro/Mobile/Distance Field Overlay");

            foreach (Graphic graphic in _uiCanvas.GetComponentsInChildren<Graphic>(true))
            {
                Material material = graphic.materialForRendering;
                if (material != null)
                {
                    if (!_uiMaterialMappings.TryGetValue(material, out Material materialCopy))
                    {
                        materialCopy = new Material(material);

                        if (material.shader.name == "TextMeshPro/Distance Field")
                        {
                            materialCopy.shader = textMeshProOverlayShader;
                        }
                        else if (material.shader.name == "TextMeshPro/Mobile/Distance Field")
                        {
                            materialCopy.shader = textMeshProMobileOverlayShader;
                        }
                        else
                        {
                            materialCopy.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                        }

                        _uiMaterialMappings.Add(material, materialCopy);
                    }

                    if (graphic is TMP_Text tmpText)
                    {
                        tmpText.fontMaterial = materialCopy;
                    }
                    else
                    {
                        graphic.material = materialCopy;
                    }
                }
            }
        }

        private void InitializeDialog(NaqueraSayDialog dialog)
        {
            _dialogCanvas = dialog.gameObject.GetComponent<Canvas>();
            _dialogCanvas.renderMode = RenderMode.WorldSpace;
            _dialogCanvas.transform.parent = transform;
            
            _dialogCanvas.transform.localRotation = Quaternion.identity;
            _dialogCanvas.transform.localScale = Vector3.one * 0.001f;
            UpdateUIPosition();

            ConversationDialoguePatcher.EnteredDialog -= InitializeDialog;
        }

        private void HandleCameraPreRender(Camera cam)
        {
            if (!_vrCameraHasAlignedToHead)
            {
                AlignCameraToHead();
                Camera.onPreRender -= HandleCameraPreRender;
            }
        }

        private void HandleCameraPreCull(Camera camera)
        {
            if (camera == _cinemachineCamera)
            {
                _cinemachineCameraCullingMask = camera.cullingMask;
                camera.cullingMask = 0;
            }
            else if (camera == _firstPersonCamera)
            {
                _firstPersonCameraCullingMask = camera.cullingMask;
                camera.cullingMask = 0;
            }
        }

        private void HandleCameraPostRender(Camera camera)
        {
            if (camera == _cinemachineCamera)
            {
                camera.cullingMask = _cinemachineCameraCullingMask;
            }
            else if (camera == _firstPersonCamera)
            {
                camera.cullingMask = _firstPersonCameraCullingMask;
            }
        }

        private void LateUpdate()
        {
            UpdateCamera();
            UpdateUI();

            if (Input.GetKeyDown(Settings.RecenterKey.Value))
            {
                AlignCameraToHead();
            }

            if (Settings.EnableTimeScaleControl.Value == true)
            {
                if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Plus))
                {
                    Time.timeScale = Mathf.Clamp(Mathf.Round(Time.timeScale + 1f), 1f, 10f);
                    Debug.Log($"Time scale: {Time.timeScale}");
                }
                else if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
                {
                    Time.timeScale = Mathf.Clamp(Mathf.Round(Time.timeScale - 1f), 1f, 10f);
                    Debug.Log($"Time scale: {Time.timeScale}");
                }
            }
        }

        private void UpdateCamera()
        {
            Camera activeCamera = CinemachineController.Instance.GetOutputCamera();
            bool firstPersonCameraIsActive = _firstPersonVirtualCamera.Equals(CinemachineController.Instance.brain.ActiveVirtualCamera);
            bool thirdPersonCameraIsActive = _thirdPersonVirtualCamera.Equals(CinemachineController.Instance.brain.ActiveVirtualCamera);

            if (_vrFirstPersonController.FullFirstPersonModeIsActive)
            {
                transform.position = _vrFirstPersonController.FirstPersonCameraPosition;
                transform.rotation = _vrFirstPersonController.FirstPersonCameraRotation;
            }
            else
            {
                Vector3 offset = thirdPersonCameraIsActive
                    ? Vector3.up * Settings.ThirdPersonCameraHeight.Value
                    : Vector3.zero;

                transform.position = activeCamera.transform.position + offset;

                if (firstPersonCameraIsActive || thirdPersonCameraIsActive)
                {
                    transform.rotation = Quaternion.Euler(0f, activeCamera.transform.rotation.eulerAngles.y, activeCamera.transform.rotation.eulerAngles.z);
                }
                else
                {
                    transform.rotation = activeCamera.transform.rotation;
                }
            }

            _vrCamera.nearClipPlane = activeCamera.nearClipPlane;
            _vrCamera.farClipPlane = activeCamera.farClipPlane;

            _vrCamera.cullingMask = _cinemachineCameraCullingMask;
            if (firstPersonCameraIsActive)
            {
                _vrCamera.cullingMask |= _firstPersonCameraCullingMask;
            }

            transform.position += _vrPositionOffset;
        }

        private void UpdateUI()
        {
            if (Input.GetKeyDown(Settings.ToggleHUDKey.Value))
            {
                Settings.ShowHUD.Value = !Settings.ShowHUD.Value;
            }

            if (Settings.ShowHUD.Value == true)
            {
                // This breaks smooth transitions (e.g. when pausing/unpausing), but it's good enough for now.
                _inGameViewCanvasGroup.alpha = _inGameViewController.IsActive
                    ? 1f
                    : 0f;
            }
            else
            {
                _inGameViewCanvasGroup.alpha = 0f;
            }

            if (_newspaperTransform.gameObject.activeInHierarchy)
            {
                // The newspaper's Z scale is 0, which breaks text rendering in VR (probably because it's rendered in
                // world space). Force it to use a very small Z scale instead.
                Vector3 scale = _newspaperTransform.localScale;
                scale.z = 0.001f;
                _newspaperTransform.localScale = scale;
            }

            bool isEndGameCutsceneActive = _endGameCutscene.gameObject.activeInHierarchy;
            if (_endGameCanvas.gameObject.activeInHierarchy != isEndGameCutsceneActive)
            {
                _endGameCanvas.gameObject.SetActive(isEndGameCutsceneActive);
            }
        }

        private void UpdateUIPosition()
        {
            Vector3 position = new Vector3(0f, -_vrPositionOffset.y, 2f);

            _uiCanvas.transform.localPosition = position;
            _endGameCanvas.transform.localPosition = position;

            if (_dialogCanvas != null)
            {
                _dialogCanvas.transform.localPosition = position;
            }
        }

        private void AlignCameraToHead()
        {
            // Calling InputTracking.Recenter() is deprecated and doesn't do anything, and I'm not able to find or
            // initialize an XRInputSubsystem to call TryRecenter() on. Guess we'll have to settle for this.
            _vrPositionOffset = transform.position - _vrCamera.transform.position;
            _vrCameraHasAlignedToHead = true;

            UpdateUIPosition();
        }
    }
}
