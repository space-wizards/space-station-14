using Content.Shared.Input;
using Robust.Shared.Input;

namespace Content.Client.Input
{
    /// <summary>
    ///     Contains a helper function for setting up all content
    ///     contexts, and modifying existing engine ones.
    /// </summary>
    public static class ContentContexts
    {
        public static void SetupContexts(IInputContextContainer contexts)
        {
            var common = contexts.GetContext("common");
            common.AddFunction(ContentKeyFunctions.FocusChat);
            common.AddFunction(ContentKeyFunctions.FocusLocalChat);
            common.AddFunction(ContentKeyFunctions.FocusEmote);
            common.AddFunction(ContentKeyFunctions.FocusWhisperChat);
            common.AddFunction(ContentKeyFunctions.FocusRadio);
            common.AddFunction(ContentKeyFunctions.FocusLOOC);
            common.AddFunction(ContentKeyFunctions.FocusOOC);
            common.AddFunction(ContentKeyFunctions.FocusAdminChat);
            common.AddFunction(ContentKeyFunctions.FocusConsoleChat);
            common.AddFunction(ContentKeyFunctions.FocusDeadChat);
            common.AddFunction(ContentKeyFunctions.CycleChatChannelForward);
            common.AddFunction(ContentKeyFunctions.CycleChatChannelBackward);
            common.AddFunction(ContentKeyFunctions.EscapeContext);
            common.AddFunction(ContentKeyFunctions.ExamineEntity);
            common.AddFunction(ContentKeyFunctions.OpenAHelp);
            common.AddFunction(ContentKeyFunctions.TakeScreenshot);
            common.AddFunction(ContentKeyFunctions.TakeScreenshotNoUI);
            common.AddFunction(ContentKeyFunctions.ToggleFullscreen);
            common.AddFunction(ContentKeyFunctions.MoveStoredItem);
            common.AddFunction(ContentKeyFunctions.RotateStoredItem);
            common.AddFunction(ContentKeyFunctions.SaveItemLocation);
            common.AddFunction(ContentKeyFunctions.Point);
            common.AddFunction(ContentKeyFunctions.ZoomOut);
            common.AddFunction(ContentKeyFunctions.ZoomIn);
            common.AddFunction(ContentKeyFunctions.ResetZoom);
            common.AddFunction(ContentKeyFunctions.InspectEntity);
            common.AddFunction(ContentKeyFunctions.InspectServerComponent);
            common.AddFunction(ContentKeyFunctions.InspectClientComponent);
            common.AddFunction(ContentKeyFunctions.ToggleRoundEndSummaryWindow);

            // Not in engine, because engine cannot check for sanbox/admin status before starting placement.
            common.AddFunction(ContentKeyFunctions.EditorCopyObject);

            // Not in engine because the engine doesn't understand what a flipped object is
            common.AddFunction(ContentKeyFunctions.EditorFlipObject);

            // Not in engine so that the RCD can rotate objects
            common.AddFunction(EngineKeyFunctions.EditorRotateObject);

            // actions should be common (for ghosts, mobs, etc)
            common.AddFunction(ContentKeyFunctions.OpenActionsMenu);

            foreach (var boundKey in ContentKeyFunctions.GetHotbarBoundKeys())
            {
                common.AddFunction(boundKey);
            }

            common.AddFunction(ContentKeyFunctions.OpenEntitySpawnWindow);
            common.AddFunction(ContentKeyFunctions.OpenSandboxWindow);
            common.AddFunction(ContentKeyFunctions.OpenTileSpawnWindow);
            common.AddFunction(ContentKeyFunctions.OpenDecalSpawnWindow);
            common.AddFunction(ContentKeyFunctions.OpenAdminMenu);
            common.AddFunction(ContentKeyFunctions.OpenGuidebook);

            var human = contexts.GetContext("human");
            var aghost = contexts.New("aghost", "common");

            // Key functions shared between human and aghost
            IEnumerable<BoundKeyFunction> sharedKeyFunctions =
            [
                EngineKeyFunctions.MoveUp,
                EngineKeyFunctions.MoveDown,
                EngineKeyFunctions.MoveLeft,
                EngineKeyFunctions.MoveRight,
                EngineKeyFunctions.Walk,
                ContentKeyFunctions.SwapHands,
                ContentKeyFunctions.SwapHandsReverse,
                ContentKeyFunctions.Drop,
                ContentKeyFunctions.UseItemInHand,
                ContentKeyFunctions.AltUseItemInHand,
                ContentKeyFunctions.OpenCharacterMenu,
                ContentKeyFunctions.ActivateItemInWorld,
                ContentKeyFunctions.ThrowItemInHand,
                ContentKeyFunctions.AltActivateItemInWorld,
                ContentKeyFunctions.TryPullObject,
                ContentKeyFunctions.MovePulledObject,
                ContentKeyFunctions.ReleasePulledObject,
                ContentKeyFunctions.OpenCraftingMenu,
                ContentKeyFunctions.OpenInventoryMenu,
                ContentKeyFunctions.SmartEquipBackpack,
                ContentKeyFunctions.SmartEquipBelt,
                ContentKeyFunctions.SmartEquipPocket1,
                ContentKeyFunctions.SmartEquipPocket2,
                ContentKeyFunctions.SmartEquipSuitStorage,
                ContentKeyFunctions.OpenBackpack,
                ContentKeyFunctions.OpenBelt,
                ContentKeyFunctions.RotateObjectClockwise,
                ContentKeyFunctions.RotateObjectCounterclockwise,
                ContentKeyFunctions.FlipObject,
                ContentKeyFunctions.ArcadeUp,
                ContentKeyFunctions.ArcadeDown,
                ContentKeyFunctions.ArcadeLeft,
                ContentKeyFunctions.ArcadeRight,
                ContentKeyFunctions.Arcade1,
                ContentKeyFunctions.Arcade2,
                ContentKeyFunctions.Arcade3
            ];

            foreach (var keyFunction in sharedKeyFunctions)
            {
                human.AddFunction(keyFunction);
                aghost.AddFunction(keyFunction);
            }
            human.AddFunction(ContentKeyFunctions.ToggleKnockdown);
            human.AddFunction(ContentKeyFunctions.OpenEmotesMenu);

            var ghost = contexts.New("ghost", "human");
            ghost.AddFunction(EngineKeyFunctions.MoveUp);
            ghost.AddFunction(EngineKeyFunctions.MoveDown);
            ghost.AddFunction(EngineKeyFunctions.MoveLeft);
            ghost.AddFunction(EngineKeyFunctions.MoveRight);
            ghost.AddFunction(EngineKeyFunctions.Walk);

        }
    }
}
