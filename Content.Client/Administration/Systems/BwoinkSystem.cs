#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Client.Administration.Managers;
using Content.Client.Administration.UI;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.HUD;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client.Administration.Systems
{
    [UsedImplicitly]
    public sealed class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IGameHud _hud = default!;

        private BwoinkWindow? _adminWindow;
        private DefaultWindow? _plainWindow;
        private readonly Dictionary<NetUserId, BwoinkPanel> _activePanelMap = new();

        public bool IsOpen => (_adminWindow?.IsOpen ?? false) || (_plainWindow?.IsOpen ?? false);

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
                SoundSystem.Play("/Audio/Effects/adminhelp.ogg", Filter.Local());
                _clyde.RequestWindowAttention();
            }

            // If they're not an admin force it open so they read
            // If it's admin-admin messaging then eh.
            if (!_adminManager.HasFlag(AdminFlags.Adminhelp))
                _plainWindow?.Open();
            else
            {
                _adminWindow?.OnBwoink(message.ChannelId);

                if (_adminWindow?.IsOpen != true)
                    _hud.SetInfoRed(true);
            }
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

            _hud.SetInfoRed(false);

            if (_adminManager.HasFlag(AdminFlags.Adminhelp))
            {
                SelectChannel(channelId.Value);
                _adminWindow?.Open();
                return;
            }

            EnsurePlain(channelId.Value);
            _plainWindow?.Open();
        }

        public void Close()
        {
            _adminWindow?.Close();
            _plainWindow?.Close();
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

