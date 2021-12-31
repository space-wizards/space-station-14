#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.Administration.UI;
using Content.Client.Administration.UI.CustomControls;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Localization;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Client.Administration
{
    [UsedImplicitly]
    public class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;

        private BwoinkWindow? _adminWindow;
        private BwoinkedWindow? _plainWindow;
        private readonly Dictionary<NetUserId, BwoinkPanel> _activePanelMap = new();

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnBwoinkTextMessage(message, eventArgs);
            LogBwoink(message);
            // Actual line
            var window = EnsurePanel(message.ChannelId);
            window.ReceiveLine($"[color=gray]{message.SentAt.ToShortTimeString()}[/color] {message.Text}");
            // Play a sound if we didn't send it
            var localPlayer = _playerManager.LocalPlayer;
            if (localPlayer?.UserId != message.TrueSender)
            {
                SoundSystem.Play(Filter.Local(), "/Audio/Effects/adminhelp.ogg");
                _clyde.RequestWindowAttention();
            }

            _adminWindow?.ChannelSelector?.Refresh();
        }

        public bool TryGetChannel(NetUserId ch, [NotNullWhen(true)] out BwoinkPanel bp) => _activePanelMap.TryGetValue(ch, out bp!);

        private BwoinkPanel EnsureAdmin(NetUserId channelId)
        {
            if (_adminWindow is null)
            {
                _adminWindow = new BwoinkWindow(this);
            }

            if (!_activePanelMap.TryGetValue(channelId, out var existingPanel))
            {
                _playerManager.SessionsDict.TryGetValue(channelId, out var otherSession);
                existingPanel = new BwoinkPanel(this, channelId, otherSession?.Name ?? channelId.ToString());
                _activePanelMap[channelId] = existingPanel;
            }

            if (!_adminWindow.IsOpen)
            {
                _adminWindow.Open();

                var csl = _adminWindow.ChannelSelector.PlayerItemList;
                csl.ClearSelected();

                var pi = csl.FirstOrDefault(i => ((PlayerInfo) i.Metadata!).SessionId == channelId);
                if (pi is not null)
                    pi.Selected = true;
            }
            return existingPanel;
        }

        private BwoinkPanel EnsurePlain(NetUserId channelId)
        {
            if (_plainWindow is null)
            {
                var bp = new BwoinkPanel(this, channelId, Loc.GetString("bwoink-user-title"));
                _plainWindow = new BwoinkedWindow(bp);
            }

            _plainWindow.Open();
            return _plainWindow.Bwoink!;
        }

        public BwoinkPanel EnsurePanel(NetUserId channelId)
        {
            if (_adminManager.HasFlag(AdminFlags.Adminhelp))
                return EnsureAdmin(channelId);

            return EnsurePlain(channelId);
        }

        public void EnsurePanelForLocalPlayer()
        {
            var localPlayer = _playerManager.LocalPlayer;
            if (localPlayer != null)
                EnsurePanel(localPlayer.UserId);
        }

        public void Send(NetUserId channelId, string text)
        {
            // Reuse the channel ID as the 'true sender'.
            // Server will ignore this and if someone makes it not ignore this (which is bad, allows impersonation!!!), that will help.
            RaiseNetworkEvent(new BwoinkTextMessage(channelId, channelId, text));
        }
    }
}

