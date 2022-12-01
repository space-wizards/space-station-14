using Content.Client.Eui;
using Content.Shared.Ghost.Roles.UI;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.Ghost.Roles.UI
{
    [UsedImplicitly]
    public sealed class GhostRoleWhitelistEui : BaseEui
    {
        private readonly GhostRoleWhitelistWindow _window;

        public GhostRoleWhitelistEui()
        {
            _window = new GhostRoleWhitelistWindow();

            _window.DenyButton.OnPressed += _ =>
            {
                SendMessage(new GhostRoleWhitelistChoiceMessage(GhostRoleWhitelistUiButton.Deny));
                _window.Close();
            };

            _window.AcceptButton.OnPressed += _ =>
            {
                SendMessage(new GhostRoleWhitelistChoiceMessage(GhostRoleWhitelistUiButton.Accept));
                _window.Close();
            };
        }

        public override void Opened()
        {
            IoCManager.Resolve<IClyde>().RequestWindowAttention();
            _window.OpenCentered();
        }

        public override void Closed()
        {
            _window.Close();
        }

    }
}
