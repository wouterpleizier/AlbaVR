using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace AlbaVR
{
    public class VRImageCarousel : MonoBehaviour
    {
        private bool _hasGameplayStarted;

        private SignCarouselController _signCarouselController;
        private CarouselScrollViewControllerLandscape _carouselScrollViewController;
        private CarouselImagePlaceholder _carouselImagePlaceholder;
        private CarouselImage _currentlyHeldCarouselImage;
        private CanvasGroup _currentlyHeldCarouselImageCanvasGroup;
        private CarouselImageContent _currentlyHeldCarouselImageContent;
        private PhotoPrintSlot _currentlyHeldCarouselTargetSlot;
        private FieldInfo _carouselImageSlotField;

        private Image _carouselImageIncorrectOverlay;
        private Image _carouselImageCorrectOverlay;
        private Image _carouselImageIncorrectTag;
        private Image _carouselImageCorrectTag;

        private CarouselImageContent _carouselImageProxy;
        private Image _carouselImageProxyIncorrectOverlay;
        private Image _carouselImageProxyCorrectOverlay;
        private Image _carouselImageProxyIncorrectTag;
        private Image _carouselImageProxyCorrectTag;

        private const float _carouselImageProxyScale = 0.001f;
        private const float _carouselImageProxyForwardOffset = 0.25f;

        private void Start()
        {
            Singleton<GlobalActionService>.Instance.OnGameplayStarted += Initialize;
        }

        private void Initialize()
        {
            _signCarouselController = FindObjectOfType<SignCarouselController>();
            _carouselScrollViewController = FindObjectOfType<CarouselScrollViewControllerLandscape>();
            _carouselImagePlaceholder = _carouselScrollViewController.GetComponentInChildren<CarouselImagePlaceholder>();

            _carouselImageSlotField = typeof(CarouselImage).GetField("_previousSlot", BindingFlags.NonPublic | BindingFlags.Instance);

            _hasGameplayStarted = true;
        }

        private void LateUpdate()
        {
            if (!_hasGameplayStarted)
            {
                return;
            }

            UpdateCarouselImageData();
            UpdateCarouselImageProxy();
        }

        private void UpdateCarouselImageData()
        {
            // The placeholder's state tells us when a carousel image is selected and being moved. Note that we're also
            // checking the parent transform, as the placeholder starts out active and parented to something else when
            // the player hasn't used the carousel yet.
            if (_carouselImagePlaceholder.isActiveAndEnabled && _carouselImagePlaceholder.transform.parent.parent == _carouselScrollViewController.transform)
            {
                if (_currentlyHeldCarouselImage == null || _currentlyHeldCarouselImage.transform.parent != _signCarouselController.transform)
                {
                    _currentlyHeldCarouselImage = _signCarouselController.transform.Find("CarouselImage(Clone)")?.GetComponent<CarouselImage>();
                }
            }
            else if (_currentlyHeldCarouselImage != null)
            {
                _currentlyHeldCarouselImage = null;

                if (_currentlyHeldCarouselImageCanvasGroup != null)
                {
                    _currentlyHeldCarouselImageCanvasGroup.alpha = 1f;
                }
            }

            if (_currentlyHeldCarouselImage != null)
            {
                if (_currentlyHeldCarouselImageCanvasGroup == null || _currentlyHeldCarouselImageCanvasGroup.transform != _currentlyHeldCarouselImage.transform)
                {
                    _currentlyHeldCarouselImageCanvasGroup = _currentlyHeldCarouselImage.gameObject.GetComponent<CanvasGroup>();

                    if (_currentlyHeldCarouselImageCanvasGroup != null)
                    {
                        _currentlyHeldCarouselImageCanvasGroup.alpha = 0f;
                    }
                }

                if (_currentlyHeldCarouselImageContent == null || _currentlyHeldCarouselImageContent.transform.parent != _currentlyHeldCarouselImage.transform)
                {
                    _currentlyHeldCarouselImageContent = _currentlyHeldCarouselImage.gameObject.GetComponentInChildren<CarouselImageContent>();
                }

                _currentlyHeldCarouselTargetSlot = _carouselImageSlotField.GetValue(_currentlyHeldCarouselImage) as PhotoPrintSlot;
            }
            else
            {
                _currentlyHeldCarouselImageCanvasGroup = null;
                _currentlyHeldCarouselImageContent = null;
                _currentlyHeldCarouselTargetSlot = null;
            }
        }

        private void UpdateCarouselImageProxy()
        {
            bool enableCarouselImageProxy = false;

            if (_currentlyHeldCarouselImage != null)
            {
                SpawnOrSynchronizeCarouselImageProxy(_currentlyHeldCarouselImage);

                if (_currentlyHeldCarouselTargetSlot != null)
                {
                    _carouselImageProxy.transform.position = _currentlyHeldCarouselTargetSlot.transform.position + (_currentlyHeldCarouselTargetSlot.transform.forward * _carouselImageProxyForwardOffset);
                    _carouselImageProxy.transform.rotation = _currentlyHeldCarouselTargetSlot.transform.rotation;
                    _carouselImageProxy.transform.Rotate(180f, 0f, 0f);
                }
                else
                {
                    _carouselImageProxy.transform.position = _carouselImagePlaceholder.transform.position + (_carouselImagePlaceholder.transform.forward * -_carouselImageProxyForwardOffset);
                    _carouselImageProxy.transform.rotation = _carouselImagePlaceholder.transform.rotation;
                }

                // Apply the rotation and scale from the actual carousel image onto the proxy too. These values are
                // animated when attempting to place the photo onto the sign.
                _carouselImageProxy.transform.localRotation *= _currentlyHeldCarouselImageContent.transform.localRotation;
                _carouselImageProxy.transform.localScale = _carouselImageProxyScale * (_currentlyHeldCarouselImageContent.transform.localScale / 0.75f);

                enableCarouselImageProxy = true;
            }

            if (_carouselImageProxy != null)
            {
                _carouselImageProxy.gameObject.SetActive(enableCarouselImageProxy);
            }
        }

        private void SpawnOrSynchronizeCarouselImageProxy(CarouselImage carouselImage)
        {
            if (carouselImage == null)
            {
                return;
            }

            CarouselImageContent originalCarouselImage = carouselImage.GetComponentInChildren<CarouselImageContent>();
            _carouselImageIncorrectOverlay = originalCarouselImage.transform.Find("Image/Incorrect").GetComponent<Image>();
            _carouselImageCorrectOverlay = originalCarouselImage.transform.Find("Image/Correct").GetComponent<Image>();
            _carouselImageIncorrectTag = originalCarouselImage.transform.Find("Image/Tag/Incorrect").GetComponent<Image>();
            _carouselImageCorrectTag = originalCarouselImage.transform.Find("Image/Tag/Correct").GetComponent<Image>();

            if (_carouselImageProxy == null)
            {
                GameObject carouselImageProxyObject = Instantiate(originalCarouselImage.gameObject);
                _carouselImageProxy = carouselImageProxyObject.GetComponent<CarouselImageContent>();

                _carouselImageProxyIncorrectOverlay = _carouselImageProxy.transform.Find("Image/Incorrect").GetComponent<Image>();
                _carouselImageProxyCorrectOverlay = _carouselImageProxy.transform.Find("Image/Correct").GetComponent<Image>();
                _carouselImageProxyIncorrectTag = _carouselImageProxy.transform.Find("Image/Tag/Incorrect").GetComponent<Image>();
                _carouselImageProxyCorrectTag = _carouselImageProxy.transform.Find("Image/Tag/Correct").GetComponent<Image>();

                Destroy(carouselImageProxyObject.GetComponent<ShowAndHideCanvas>());
                Destroy(carouselImageProxyObject.GetComponent<Animator>());
                Destroy(carouselImageProxyObject.GetComponent<ExcludeFromNaqueraDistanceDisable>());

                carouselImageProxyObject.AddComponent<CanvasRenderer>();
                carouselImageProxyObject.AddComponent<Canvas>();

                carouselImageProxyObject.transform.localScale = Vector3.one * 0.001f;
            }
            else
            {
                _carouselImageProxy.SetPhotoData(carouselImage.photoData);

                _carouselImageProxyIncorrectOverlay.color = _carouselImageIncorrectOverlay.color;
                _carouselImageProxyCorrectOverlay.color = _carouselImageCorrectOverlay.color;
                _carouselImageProxyIncorrectTag.gameObject.SetActive(_carouselImageIncorrectTag.gameObject.activeSelf);
                _carouselImageProxyCorrectTag.gameObject.SetActive(_carouselImageCorrectTag.gameObject.activeSelf);
            }
        }
    }
}
