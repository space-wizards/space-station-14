#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Client.UserInterface;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.Localization;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.IoC;

namespace Content.Client.Administration
{
    [UsedImplicitly]
    public class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        private readonly Dictionary<NetUserId, BwoinkWindow> _activeWindowMap = new();

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnBwoinkTextMessage(message, eventArgs);
            LogBwoink(message);
            // TODO: Well, for one, a sound
            var window = EnsureWindow(message.ChannelId);
            window.ReceiveLine(message.Text);
        }

        public BwoinkWindow EnsureWindow(NetUserId channelId)
        {
            if (_activeWindowMap.TryGetValue(channelId, out var existingWindow))
            {
                existingWindow.Open();
                return existingWindow;
            }
            string title;
            if (_playerManager.SessionsDict.TryGetValue(channelId, out var otherSession))
            {
                title = otherSession.Name;
            }
            else
            {
                title = channelId.ToString();
            }
            var window = new BwoinkWindow(channelId, title);
            _activeWindowMap[channelId] = window;
            window.Open();
            return window;
        }

        public void EnsureWindowForLocalPlayer()
        {
            var localPlayer = _playerManager.LocalPlayer;
            if (localPlayer != null)
                EnsureWindow(localPlayer.UserId);
        }

        public void Send(NetUserId channelId, string text)
        {
            RaiseNetworkEvent(new BwoinkTextMessage(channelId, text));
        }
    }
}

