using Content.Shared.Eui;
using Content.Shared.Ghost.Roles.UI;
using Content.Server.EUI;
using Content.Server.Books;
using Robust.Server.Player;

namespace Content.Server.Ghost.Roles.UI
{
    public sealed class GhostRoleWhitelistEui : BaseEui
    {
        private readonly BookSystem _books = default!;
        private IPlayerSession _player = default!;
        public GhostRoleWhitelistEui(BookSystem books, IPlayerSession player)
        {
            _books = books;
            _player = player;
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            if (msg is not GhostRoleWhitelistChoiceMessage choice ||
                choice.Button == GhostRoleWhitelistUiButton.Deny)
            {
                Close();
                return;
            }

            // TODO: this will be a cvar in a few days when PJB pushes the launcer update
            _books.OpenURL(_player, "https://discord.gg/s7Er9mejpp");
            Close();
        }
    }
}
