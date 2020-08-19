#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Disposal
{
    public class SharedDisposalTaggerComponent : Component
    {
        public override string Name => "DisposalTagger";

        [Serializable, NetSerializable]
        public class DisposalTaggerUserInterfaceState : BoundUserInterfaceState
        {
            public readonly string Tag;

            public DisposalTaggerUserInterfaceState(string tag)
            {
                Tag = tag;
            }
        }

        [Serializable, NetSerializable]
        public class UiActionMessage : BoundUserInterfaceMessage
        {
            public readonly UiAction Action;
            public readonly string Tag = "";

            public UiActionMessage(UiAction action, string tag)
            {
                Action = action;

                if (Action == UiAction.Ok)
                {
                    Tag = tag;
                }
            }
        }

        [Serializable, NetSerializable]
        public enum UiAction
        {
            Ok
        }

        [Serializable, NetSerializable]
        public enum DisposalTaggerUiKey
        {
            Key
        }
    }
}
