using Fungus;
using HarmonyLib;

namespace AlbaVR
{
    [HarmonyPatch]
    public class ConversationDialoguePatcher
    {
        public delegate void EnteredDialogHandler(NaqueraSayDialog dialog);

        public static event EnteredDialogHandler EnteredDialog;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ConversationDialogue), "OnEnter")]
        private static void InvokeEnteredDialogEvent(NaqueraSayDialog ____sayDialog)
        {
            // ConversationDialogue has a static member named _sayDialog that gets initialized the first time OnEnter()
            // is called. Pass its value via this event so we can do stuff with it.
            EnteredDialog?.Invoke(____sayDialog);
        }
    }
}
