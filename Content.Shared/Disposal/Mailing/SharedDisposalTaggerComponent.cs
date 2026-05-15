using System.Text.RegularExpressions;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Components
{
    public sealed partial class SharedDisposalTaggerComponent : Component
    {
        public static readonly Regex TagRegex = new("^[a-zA-Z0-9 ]*$", RegexOptions.Compiled);

        [Serializable, NetSerializable]
        public sealed class DisposalTaggerUserInterfaceState : BoundUserInterfaceState
        {
            public readonly string Tag;

            public DisposalTaggerUserInterfaceState(string tag)
            {
                Tag = tag;
            }
        }

        [Serializable, NetSerializable]
        public sealed class UiActionMessage : BoundUserInterfaceMessage
        {
            public readonly UiAction Action;
            public readonly string Tag = "";

            public UiActionMessage(UiAction action, string tag)
            {
                Action = action;

                if (Action == UiAction.Ok)
                {
                    Tag = tag.Substring(0, Math.Min(tag.Length, 30));
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
