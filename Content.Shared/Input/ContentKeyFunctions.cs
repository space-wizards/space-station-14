using Robust.Shared.Input;

namespace Content.Shared.Input
{
    [KeyFunctions]
    public static class ContentKeyFunctions
    {
        public static readonly BoundKeyFunction UseItemInHand = "ActivateItemInHand";
        public static readonly BoundKeyFunction AltUseItemInHand = "AltActivateItemInHand";
        public static readonly BoundKeyFunction ActivateItemInWorld = "ActivateItemInWorld";
        public static readonly BoundKeyFunction AltActivateItemInWorld = "AltActivateItemInWorld";
        public static readonly BoundKeyFunction Drop = "Drop";
        public static readonly BoundKeyFunction ExamineEntity = "ExamineEntity";
        public static readonly BoundKeyFunction FocusChat = "FocusChatInputWindow";
        public static readonly BoundKeyFunction FocusLocalChat = "FocusLocalChatWindow";
        public static readonly BoundKeyFunction FocusEmote = "FocusEmote";
        public static readonly BoundKeyFunction FocusWhisperChat = "FocusWhisperChatWindow";
        public static readonly BoundKeyFunction FocusRadio = "FocusRadioWindow";
        public static readonly BoundKeyFunction FocusLOOC = "FocusLOOCWindow";
        public static readonly BoundKeyFunction FocusOOC = "FocusOOCWindow";
        public static readonly BoundKeyFunction FocusAdminChat = "FocusAdminChatWindow";
        public static readonly BoundKeyFunction FocusDeadChat = "FocusDeadChatWindow";
        public static readonly BoundKeyFunction FocusConsoleChat = "FocusConsoleChatWindow";
        public static readonly BoundKeyFunction CycleChatChannelForward = "CycleChatChannelForward";
        public static readonly BoundKeyFunction CycleChatChannelBackward = "CycleChatChannelBackward";
        public static readonly BoundKeyFunction EscapeContext = "EscapeContext";
        public static readonly BoundKeyFunction OpenCharacterMenu = "OpenCharacterMenu";
        public static readonly BoundKeyFunction OpenCraftingMenu = "OpenCraftingMenu";
        public static readonly BoundKeyFunction OpenGuidebook = "OpenGuidebook";
        public static readonly BoundKeyFunction OpenInventoryMenu = "OpenInventoryMenu";
        public static readonly BoundKeyFunction SmartEquipBackpack = "SmartEquipBackpack";
        public static readonly BoundKeyFunction SmartEquipBelt = "SmartEquipBelt";
        public static readonly BoundKeyFunction OpenAHelp = "OpenAHelp";
        public static readonly BoundKeyFunction SwapHands = "SwapHands";
        public static readonly BoundKeyFunction MoveStoredItem = "MoveStoredItem";
        public static readonly BoundKeyFunction RotateStoredItem = "RotateStoredItem";
        public static readonly BoundKeyFunction ThrowItemInHand = "ThrowItemInHand";
        public static readonly BoundKeyFunction TryPullObject = "TryPullObject";
        public static readonly BoundKeyFunction MovePulledObject = "MovePulledObject";
        public static readonly BoundKeyFunction ReleasePulledObject = "ReleasePulledObject";
        public static readonly BoundKeyFunction MouseMiddle = "MouseMiddle";
        public static readonly BoundKeyFunction OpenEntitySpawnWindow = "OpenEntitySpawnWindow";
        public static readonly BoundKeyFunction OpenSandboxWindow = "OpenSandboxWindow";
        public static readonly BoundKeyFunction OpenTileSpawnWindow = "OpenTileSpawnWindow";
        public static readonly BoundKeyFunction OpenDecalSpawnWindow = "OpenDecalSpawnWindow";
        public static readonly BoundKeyFunction OpenAdminMenu = "OpenAdminMenu";
        public static readonly BoundKeyFunction TakeScreenshot = "TakeScreenshot";
        public static readonly BoundKeyFunction TakeScreenshotNoUI = "TakeScreenshotNoUI";
        public static readonly BoundKeyFunction ToggleFullscreen = "ToggleFullscreen";
        public static readonly BoundKeyFunction Point = "Point";
        public static readonly BoundKeyFunction ZoomOut = "ZoomOut";
        public static readonly BoundKeyFunction ZoomIn = "ZoomIn";
        public static readonly BoundKeyFunction ResetZoom = "ResetZoom";

        public static readonly BoundKeyFunction ArcadeUp = "ArcadeUp";
        public static readonly BoundKeyFunction ArcadeDown = "ArcadeDown";
        public static readonly BoundKeyFunction ArcadeLeft = "ArcadeLeft";
        public static readonly BoundKeyFunction ArcadeRight = "ArcadeRight";
        public static readonly BoundKeyFunction Arcade1 = "Arcade1";
        public static readonly BoundKeyFunction Arcade2 = "Arcade2";
        public static readonly BoundKeyFunction Arcade3 = "Arcade3";

        public static readonly BoundKeyFunction OpenActionsMenu = "OpenAbilitiesMenu";
        public static readonly BoundKeyFunction ShuttleStrafeLeft = "ShuttleStrafeLeft";
        public static readonly BoundKeyFunction ShuttleStrafeUp = "ShuttleStrafeUp";
        public static readonly BoundKeyFunction ShuttleStrafeRight = "ShuttleStrafeRight";
        public static readonly BoundKeyFunction ShuttleStrafeDown = "ShuttleStrafeDown";
        public static readonly BoundKeyFunction ShuttleRotateLeft = "ShuttleRotateLeft";
        public static readonly BoundKeyFunction ShuttleRotateRight = "ShuttleRotateRight";
        public static readonly BoundKeyFunction ShuttleBrake = "ShuttleBrake";

        public static readonly BoundKeyFunction Hotbar0 = "Hotbar0";
        public static readonly BoundKeyFunction Hotbar1 = "Hotbar1";
        public static readonly BoundKeyFunction Hotbar2 = "Hotbar2";
        public static readonly BoundKeyFunction Hotbar3 = "Hotbar3";
        public static readonly BoundKeyFunction Hotbar4 = "Hotbar4";
        public static readonly BoundKeyFunction Hotbar5 = "Hotbar5";
        public static readonly BoundKeyFunction Hotbar6 = "Hotbar6";
        public static readonly BoundKeyFunction Hotbar7 = "Hotbar7";
        public static readonly BoundKeyFunction Hotbar8 = "Hotbar8";
        public static readonly BoundKeyFunction Hotbar9 = "Hotbar9";

        public static BoundKeyFunction[] GetHotbarBoundKeys() =>
            new[]
            {
                Hotbar1, Hotbar2, Hotbar3, Hotbar4, Hotbar5, Hotbar6, Hotbar7, Hotbar8, Hotbar9, Hotbar0
            };

        public static readonly BoundKeyFunction Vote0 = "Vote0";
        public static readonly BoundKeyFunction Vote1 = "Vote1";
        public static readonly BoundKeyFunction Vote2 = "Vote2";
        public static readonly BoundKeyFunction Vote3 = "Vote3";
        public static readonly BoundKeyFunction Vote4 = "Vote4";
        public static readonly BoundKeyFunction Vote5 = "Vote5";
        public static readonly BoundKeyFunction Vote6 = "Vote6";
        public static readonly BoundKeyFunction Vote7 = "Vote7";
        public static readonly BoundKeyFunction Vote8 = "Vote8";
        public static readonly BoundKeyFunction Vote9 = "Vote9";
        public static readonly BoundKeyFunction EditorCopyObject = "EditorCopyObject";
        public static readonly BoundKeyFunction EditorFlipObject = "EditorFlipObject";
        public static readonly BoundKeyFunction InspectEntity = "InspectEntity";
    }
}
