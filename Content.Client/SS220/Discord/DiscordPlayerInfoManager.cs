// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Discord;
using Robust.Shared.Network;

namespace Content.Client.SS220.Discord
{
    public sealed class DiscordPlayerInfoManager
    {
        [Dependency] private readonly IClientNetManager _netMgr = default!;

        private DiscordSponsorInfo? _info;

        public event Action? SponsorStatusChanged;

        public void Initialize()
        {
            _netMgr.RegisterNetMessage<MsgUpdatePlayerDiscordStatus>(UpdateSponsorStatus);
        }

        private void UpdateSponsorStatus(MsgUpdatePlayerDiscordStatus message)
        {
            _info = message.Info;

            SponsorStatusChanged?.Invoke();
        }

        public SponsorTier[] GetSponsorTier()
        {
            return _info?.Tiers ?? Array.Empty<SponsorTier>();
        }
    }
}
