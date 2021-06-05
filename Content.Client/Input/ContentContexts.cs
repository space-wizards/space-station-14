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
            common.AddFunction(ContentKeyFunctions.FocusRadio);
            common.AddFunction(ContentKeyFunctions.FocusOOC);
            common.AddFunction(ContentKeyFunctions.FocusAdminChat);
            common.AddFunction(ContentKeyFunctions.CycleChatChannelForward);
            common.AddFunction(ContentKeyFunctions.CycleChatChannelBackward);
            common.AddFunction(ContentKeyFunctions.ExamineEntity);
            common.AddFunction(ContentKeyFunctions.OpenInfo);
            common.AddFunction(ContentKeyFunctions.TakeScreenshot);
            common.AddFunction(ContentKeyFunctions.TakeScreenshotNoUI);
            common.AddFunction(ContentKeyFunctions.Point);

            var human = contexts.GetContext("human");
            human.AddFunction(ContentKeyFunctions.SwapHands);
            human.AddFunction(ContentKeyFunctions.Drop);
            human.AddFunction(ContentKeyFunctions.ActivateItemInHand);
            human.AddFunction(ContentKeyFunctions.OpenCharacterMenu);
            human.AddFunction(ContentKeyFunctions.ActivateItemInWorld);
            human.AddFunction(ContentKeyFunctions.ThrowItemInHand);
            human.AddFunction(ContentKeyFunctions.TryPullObject);
            human.AddFunction(ContentKeyFunctions.MovePulledObject);
            human.AddFunction(ContentKeyFunctions.ReleasePulledObject);
            human.AddFunction(ContentKeyFunctions.OpenContextMenu);
            human.AddFunction(ContentKeyFunctions.OpenCraftingMenu);
            human.AddFunction(ContentKeyFunctions.OpenInventoryMenu);
            human.AddFunction(ContentKeyFunctions.SmartEquipBackpack);
            human.AddFunction(ContentKeyFunctions.SmartEquipBelt);
            human.AddFunction(ContentKeyFunctions.MouseMiddle);
            human.AddFunction(ContentKeyFunctions.WideAttack);
            human.AddFunction(ContentKeyFunctions.ArcadeUp);
            human.AddFunction(ContentKeyFunctions.ArcadeDown);
            human.AddFunction(ContentKeyFunctions.ArcadeLeft);
            human.AddFunction(ContentKeyFunctions.ArcadeRight);
            human.AddFunction(ContentKeyFunctions.Arcade1);
            human.AddFunction(ContentKeyFunctions.Arcade2);
            human.AddFunction(ContentKeyFunctions.Arcade3);

            // actions should be common (for ghosts, mobs, etc)
            common.AddFunction(ContentKeyFunctions.OpenActionsMenu);
            common.AddFunction(ContentKeyFunctions.Hotbar0);
            common.AddFunction(ContentKeyFunctions.Hotbar1);
            common.AddFunction(ContentKeyFunctions.Hotbar2);
            common.AddFunction(ContentKeyFunctions.Hotbar3);
            common.AddFunction(ContentKeyFunctions.Hotbar4);
            common.AddFunction(ContentKeyFunctions.Hotbar5);
            common.AddFunction(ContentKeyFunctions.Hotbar6);
            common.AddFunction(ContentKeyFunctions.Hotbar7);
            common.AddFunction(ContentKeyFunctions.Hotbar8);
            common.AddFunction(ContentKeyFunctions.Hotbar9);
            common.AddFunction(ContentKeyFunctions.Loadout1);
            common.AddFunction(ContentKeyFunctions.Loadout2);
            common.AddFunction(ContentKeyFunctions.Loadout3);
            common.AddFunction(ContentKeyFunctions.Loadout4);
            common.AddFunction(ContentKeyFunctions.Loadout5);
            common.AddFunction(ContentKeyFunctions.Loadout6);
            common.AddFunction(ContentKeyFunctions.Loadout7);
            common.AddFunction(ContentKeyFunctions.Loadout8);
            common.AddFunction(ContentKeyFunctions.Loadout9);

            var ghost = contexts.New("ghost", "common");
            ghost.AddFunction(EngineKeyFunctions.MoveUp);
            ghost.AddFunction(EngineKeyFunctions.MoveDown);
            ghost.AddFunction(EngineKeyFunctions.MoveLeft);
            ghost.AddFunction(EngineKeyFunctions.MoveRight);
            ghost.AddFunction(EngineKeyFunctions.Walk);
            ghost.AddFunction(ContentKeyFunctions.OpenContextMenu);

            common.AddFunction(ContentKeyFunctions.OpenEntitySpawnWindow);
            common.AddFunction(ContentKeyFunctions.OpenSandboxWindow);
            common.AddFunction(ContentKeyFunctions.OpenTileSpawnWindow);
            common.AddFunction(ContentKeyFunctions.OpenAdminMenu);
        }
    }
}
