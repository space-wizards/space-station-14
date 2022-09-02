using Content.Shared.Administration;
using Robust.Client.Player;

namespace Content.Client.Administration.Systems
{
    public sealed class PrayerSystem : SharedPrayerSystem
    {
        [Dependency] private readonly BwoinkSystem _bwoinkSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            SubscribeNetworkEvent<PrayTextMessage>(OnTextPrayMessage);
        }

        protected void OnTextPrayMessage(PrayTextMessage message, EntitySessionEventArgs eventArgs)
        {
            var localPlayer = _playerManager.LocalPlayer;
            if (localPlayer != null)
                _bwoinkSystem.Send(localPlayer.UserId, "[PRAY] " + message.Text);
        }
    }
}
