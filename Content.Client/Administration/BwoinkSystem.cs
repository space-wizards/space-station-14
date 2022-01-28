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
using Robust.Client.UserInterface.CustomControls;
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
        private DefaultWindow? _plainWindow;
        private readonly Dictionary<NetUserId, BwoinkPanel> _activePanelMap = new();

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnBwoinkTextMessage(message, eventArgs);
            LogBwoink(message);
            // Actual line
            var window = EnsurePanel(message.ChannelId);
            window.ReceiveLine(message);
            // Play a sound if we didn't send it
            var localPlayer = _playerManager.LocalPlayer;
            if (localPlayer?.UserId != message.TrueSender)
            {
                SoundSystem.Play(Filter.Local(), "/Audio/Effects/adminhelp.ogg");
                _clyde.RequestWindowAttention();
            }

            _adminWindow?.OnBwoink(message.ChannelId);
        }

        public bool TryGetChannel(NetUserId ch, [NotNullWhen(true)] out BwoinkPanel? bp) => _activePanelMap.TryGetValue(ch, out bp);

        private BwoinkPanel EnsureAdmin(NetUserId channelId)
        {
            _adminWindow ??= new BwoinkWindow(this);

            if (!_activePanelMap.TryGetValue(channelId, out var existingPanel))
            {
                _activePanelMap[channelId] = existingPanel = new BwoinkPanel(this, channelId);
                existingPanel.Visible = false;
                if (!_adminWindow.BwoinkArea.Children.Contains(existingPanel))
                    _adminWindow.BwoinkArea.AddChild(existingPanel);
            }

            if(!_adminWindow.IsOpen) _adminWindow.Open();

            return existingPanel;
        }

        private BwoinkPanel EnsurePlain(NetUserId channelId)
        {
            BwoinkPanel bp;
            if (_plainWindow is null)
            {
                bp = new BwoinkPanel(this, channelId);
                _plainWindow = new DefaultWindow()
                {
                    TitleClass="windowTitleAlert",
                    HeaderClass="windowHeaderAlert",
                    Title=Loc.GetString("bwoink-user-title"),
                    SetSize=(400, 200),
                };

                _plainWindow.Contents.AddChild(bp);
            }
            else
            {
                bp = (BwoinkPanel) _plainWindow.Contents.GetChild(0);
            }

            _plainWindow.Open();
            return bp;
        }

        public BwoinkPanel EnsurePanel(NetUserId channelId)
        {
            if (_adminManager.HasFlag(AdminFlags.Adminhelp))
                return EnsureAdmin(channelId);

            return EnsurePlain(channelId);
        }

        public void Open(NetUserId? channelId = null)
        {
            if (channelId == null)
            {
                var localPlayer = _playerManager.LocalPlayer;
                if (localPlayer != null)
                    Open(localPlayer.UserId);
                return;
            }

            if (_adminManager.HasFlag(AdminFlags.Adminhelp))
            {
                SelectChannel(channelId.Value);
                return;
            }

            EnsurePlain(channelId.Value);
        }

        private void SelectChannel(NetUserId uid)
        {
            _adminWindow ??= new BwoinkWindow(this);
            _adminWindow.SelectChannel(uid);
        }

        public void Send(NetUserId channelId, string text)
        {
            // Reuse the channel ID as the 'true sender'.
            // Server will ignore this and if someone makes it not ignore this (which is bad, allows impersonation!!!), that will help.
            RaiseNetworkEvent(new BwoinkTextMessage(channelId, channelId, text));
        }
    }
}

