using System.Text.RegularExpressions;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Components
{
    public sealed partial class SharedDisposalRouterComponent : Component
    {
        public static readonly Regex TagRegex = new("^[a-zA-Z0-9, ]*$", RegexOptions.Compiled);

        [Serializable, NetSerializable]
        public sealed class DisposalRouterUserInterfaceState : BoundUserInterfaceState
        {
            public readonly string Tags;
            public readonly bool BackwardsAllowed;

            public DisposalRouterUserInterfaceState(string tags, bool backwardsAllowed = false)
            {
                Tags = tags;
                BackwardsAllowed = backwardsAllowed;
            }
        }

        [Serializable, NetSerializable]
        public sealed class UiActionMessage : BoundUserInterfaceMessage
        {
            public readonly UiAction Action;
            public readonly string Tags = "";
            public readonly bool BackwardsAllowed;

            public UiActionMessage(UiAction action, string tags, bool backwardsAllowed = false)
            {
                Action = action;

                if (Action == UiAction.Ok)
                {
                    Tags = tags.Substring(0, Math.Min(tags.Length, 150));
                    BackwardsAllowed = backwardsAllowed;
                }
            }
        }

        [Serializable, NetSerializable]
        public enum UiAction
        {
            Ok
        }

        [Serializable, NetSerializable]
        public enum DisposalRouterUiKey
        {
            Key
        }
    }
}
