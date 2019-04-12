using SS14.Shared.Input;

namespace Content.Shared.Input
{
    [KeyFunctions]
    public static class ContentKeyFunctions
    {
        public static readonly BoundKeyFunction SwapHands = "SwapHands";
        public static readonly BoundKeyFunction Drop = "Drop";
        public static readonly BoundKeyFunction ActivateItemInHand = "ActivateItemInHand";
        public static readonly BoundKeyFunction OpenCharacterMenu = "OpenCharacterMenu";
        public static readonly BoundKeyFunction ExamineEntity = "ExamineEntity";
        public static readonly BoundKeyFunction UseItemInHand = "UseItemInHand"; // use hand item on world entity
        public static readonly BoundKeyFunction ActivateItemInWorld = "ActivateItemInWorld"; // default action on world entity
        public static readonly BoundKeyFunction ThrowItemInHand = "ThrowItemInHand";
        public static readonly BoundKeyFunction OpenContextMenu = "OpenContextMenu";
        public static readonly BoundKeyFunction FocusChat = "FocusChatWindow";
    }
}
