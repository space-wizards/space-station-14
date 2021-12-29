#nullable enable
using System.Collections.Generic;
using Content.Client.Administration.UI;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Client.Administration
{
    [UsedImplicitly]
    public class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;

        private readonly Dictionary<NetUserId, BwoinkWindow> _activeWindowMap = new();

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnBwoinkTextMessage(message, eventArgs);
            LogBwoink(message);
            // Actual line
            var window = EnsureWindow(message.ChannelId);
            window.ReceiveLine(message.Text);
            // Play a sound if we didn't send it
            var localPlayer = _playerManager.LocalPlayer;
            if (localPlayer?.UserId != message.TrueSender)
            {
                SoundSystem.Play(Filter.Local(), "/Audio/Effects/adminhelp.ogg");
                _clyde.RequestWindowAttention();
            }
        }

        public BwoinkWindow EnsureWindow(NetUserId channelId)
        {
            if (!_activeWindowMap.TryGetValue(channelId, out var existingWindow))
            {
                _activeWindowMap[channelId] = existingWindow = new BwoinkWindow(channelId,
                    _playerManager.SessionsDict.TryGetValue(channelId, out var otherSession)
                        ? otherSession.Name
                        : channelId.ToString());
            }

            existingWindow.Open();
            return existingWindow;
        }

        public void EnsureWindowForLocalPlayer()
        {
            var localPlayer = _playerManager.LocalPlayer;
            if (localPlayer != null)
                EnsureWindow(localPlayer.UserId);
        }

        public void Send(NetUserId channelId, string text)
        {
            // Reuse the channel ID as the 'true sender'.
            // Server will ignore this and if someone makes it not ignore this (which is bad, allows impersonation!!!), that will help.
            RaiseNetworkEvent(new BwoinkTextMessage(channelId, channelId, text));
        }
    }
}

