// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.SS220.DiscordLink;

namespace Content.Server.SS220.Discord
{
    public sealed class DiscordLinkEui : BaseEui
    {
        private string? _linkKey;

        public DiscordLinkEui()
        {
            IoCManager.InjectDependencies(this);
        }

        public override EuiStateBase GetNewState()
        {
            return new DiscordLinkEuiState(_linkKey);
        }

        public void SetLinkKey(string? linkKey)
        {
            _linkKey = linkKey;

            StateDirty();
        }
    }
}
