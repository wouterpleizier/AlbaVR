using HarmonyLib;

namespace AlbaVR
{
    [HarmonyPatch]
    public class CarouselPatcher
    {
        private static NaqueraScrollRect _currentScrollRect;
        private static PhotoPrintSlotCollection _currentPhotoPrintSlotCollection;
        private static InputSelectableCollection _currentInputSelectableCollection;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CarouselScrollViewControllerLandscape), "EnterCarousel")]
        private static void AfterEnterCarousel(PhotoPrintSlotCollection slots, NaqueraScrollRect ____scrollRect)
        {
            _currentScrollRect = ____scrollRect;
            _currentPhotoPrintSlotCollection = slots;
            _currentInputSelectableCollection = slots.GetComponent<InputSelectableCollection>();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CarouselScrollViewControllerLandscape), "ExitCarousel")]
        private static void AfterExitCarousel()
        {
            _currentScrollRect = null;
            _currentPhotoPrintSlotCollection = null;
            _currentInputSelectableCollection = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NaqueraScrollRect), "IsCurrentIndexItemPartlyOutsideScreen")]
        private static bool OverrideIsCurrentIndexItemPartlyOutsideScreen(NaqueraScrollRect __instance, ref bool __result)
        {
            if (__instance == _currentScrollRect)
            {
                // The original method is called to determine whether the carousel should scroll to the currently
                // selected photo, and it involves a screen-space check that doesn't work properly in VR at the forest
                // and castle wildlife signs (maybe more?). The easiest workaround for this is to always return true.
                // This makes the carousel scroll more often than needed (sometimes to no effect), but that's okay.
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhotoPrintSlotCollection), "AttemptToOverOnSlot")]
        private static bool OverrideHoveredSlot(CarouselImage image, PhotoPrintSlotCollection __instance, ref PhotoPrintSlot __result, PhotoPrintSlot[] ____photoSlots)
        {
            if (__instance == _currentPhotoPrintSlotCollection)
            {
                // Turn off the highlight on all slots, same as the original method.
                foreach (PhotoPrintSlot slot in ____photoSlots)
                {
                    slot.HightlightOff();
                }

                // The original method finds the photo slot that's closest to the selected photo in screen space,
                // presumably to support dragging via touch or mouse. The screen space check doesn't always work in VR
                // (even though it should still rely on the non-VR camera's coordinate system), but fortunately we're
                // only supporting keyboard and gamepad controls here. These controls directly influence the selectable
                // collection's current item, which leads us to the same photo slot that would normally be returned.
                PhotoPrintSlot photoPrintSlot = _currentInputSelectableCollection.GetCurrentSelectable().gameObject.GetComponent<PhotoPrintSlot>();

                // Same as the original method from here on.
                if (photoPrintSlot != null)
                {
                    photoPrintSlot.OverOnSlot(image);

                    if (photoPrintSlot.CanSnapCarouselImage(image))
                    {
                        photoPrintSlot.HightlightOn();
                    }
                }

                __result = photoPrintSlot;

                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhotoPrintSlot), "CanSnapCarouselImage")]
        private static bool SkipScreenSpaceOverlap(CarouselImage image, ref bool __result, PhotoPrintSlot __instance, ISlotChecker ____slotChecker)
        {
            if (_currentInputSelectableCollection != null && _currentInputSelectableCollection.GetCurrentSelectable().gameObject.GetComponent<PhotoPrintSlot>() == __instance)
            {
                // Same as the original method, but without a screen-space overlap check.
                __result = ____slotChecker.IsPhotoCorrect(image.photoData) && !__instance.HasPhotoAssigned();
                return false;
            }

            return true;
        }
    }
}
