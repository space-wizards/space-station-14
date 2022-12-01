using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles.UI
{
    [Serializable, NetSerializable]
    public enum GhostRoleWhitelistUiButton
    {
        Deny,
        Accept,
    }

    [Serializable, NetSerializable]
    public sealed class GhostRoleWhitelistChoiceMessage : EuiMessageBase
    {
        public readonly GhostRoleWhitelistUiButton Button;

        public GhostRoleWhitelistChoiceMessage(GhostRoleWhitelistUiButton button)
        {
            Button = button;
        }
    }
}
