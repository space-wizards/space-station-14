#nullable enable
using System;
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
using Robust.Shared.Log;

namespace Content.Client.Administration
{
    [UsedImplicitly]
    public class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly AdminSystem _adminSystem = default!;
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;

        private readonly Dictionary<NetUserId, AHelpState> _state = new();
        private BwoinkWindow? _adminWindow;
        private DefaultWindow? _plainWindow;

        public override void Initialize()
        {
            base.Initialize();

            var plUpdateArmed = false;
            _adminManager.AdminStatusUpdated += () =>
            {
                Logger.DebugS("ahelp", "got ASU");

                // BwoinkWindows can only exist in one parent at a time,
                // so we'll clean up both just in case.
                _adminWindow?.Close();
                _plainWindow?.Close();

                foreach (var ch in _state)
                    ch.Value.Disassociate();

                if (!IsAdmin())
                    return;

                // If we're re-adminning, this should fetch any messages since we de-adminned.
                foreach (var ch in _state.Values)
                    ch.RequestLog();

                plUpdateArmed = true;
            };

            // AdminStatusUpdated is called before the PlayerList is updated, so we have to hack around it.
            // We do this by setting a flag (plUpdateArmed) when we're expecting a new PlayerList.
            _adminSystem.PlayerListChanged += plist =>
            {
                Logger.DebugS("ahelp", "got PLC: {0}", plUpdateArmed);
                if (!plUpdateArmed)
                    return;

                // This will also fetch the logs as part of the setup.
                foreach (var p in plist)
                    GetOrCreateChannel(p.SessionId);

                plUpdateArmed = false;
            };
        }

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnBwoinkTextMessage(message, eventArgs);
            LogBwoink(message);
            Logger.DebugS("ahelp", "got BTM: {0}/{1} {2}: {3}", message.ChannelId, message.MessageId, message.Status, message.Text);

            // Actual line
            var ch = GetOrCreateChannel(message.ChannelId);
            ch.ReceiveLine(message);

            // Always open ahelps if you're not an admin,
            // or if you are, and you don't have the window open.
            if (!IsAdmin() || !(_adminWindow?.IsOpen ?? false))
                ch.Open();

            // Play a sound if we didn't send it and it's not a replay
            var localPlayer = _playerManager.LocalPlayer;
            if (message.Status != Status.Read && localPlayer?.UserId != message.TrueSender)
            {
                SoundSystem.Play(Filter.Local(), "/Audio/Effects/adminhelp.ogg");
                _clyde.RequestWindowAttention();
            }

            _adminWindow?.OnBwoink(message);
        }

        public bool TryGetChannel(NetUserId ch, [NotNullWhen(true)] out AHelpState? ahs) => _state.TryGetValue(ch, out ahs);

        public AHelpState GetOrCreateChannel(NetUserId channelId)
        {
            if (!TryGetChannel(channelId, out var existingState))
            {
                _state[channelId] = existingState = new AHelpState(this, channelId);
                RequestLog(channelId);
            }

            return existingState;
        }

        public bool IsAdmin() => _adminManager.HasFlag(AdminFlags.Adminhelp);

        #region Windowing

        public void CreateAdminWindow() => _adminWindow ??= new BwoinkWindow(this);

        public void CreateWindow()
        {
            _plainWindow ??= new DefaultWindow()
            {
                TitleClass="windowTitleAlert",
                HeaderClass="windowHeaderAlert",
                Title=Loc.GetString("bwoink-user-title"),
                SetSize=(400, 200),
            };
        }

        public void Open(NetUserId? channelId = null)
        {
            if (channelId == null)
            {
                var localPlayer = _playerManager.LocalPlayer;
                if (localPlayer != null)
                    channelId = localPlayer.UserId;
                else
                    throw new Exception("tried to Open() a channel without a target");
            }

            GetOrCreateChannel(channelId.Value)
                .Open();
        }
        #endregion

        #region Network I/O
        public void Send(NetUserId channelId, string text)
        {
            // Reuse the channel ID as the 'true sender'.
            // Server will ignore this and if someone makes it not ignore this (which is bad, allows impersonation!!!), that will help.
            var btm = new BwoinkTextMessage(channelId, channelId, text)
            {
                Status = Status.Sent
            };
            RaiseNetworkEvent(btm);
        }

        public void RequestLog(NetUserId channelId, Guid? since = default) => RaiseNetworkEvent(new FetchBwoinkLogMessage(channelId, since));
        public void SendRead(NetUserId channelId) => RaiseNetworkEvent(new BwoinkSystem.BwoinkReadMessage(channelId));
        #endregion

        // AHelpState keeps track of the message history and various channel stats,
        // like the time of the latest unread message, how many are unread, which
        // messages are already fetched from the server, etc.
        //
        // It is also the interface between the BwoinkPanel and the BwoinkSystem.
        public class AHelpState
        {
            private BwoinkSystem _parent;
            public readonly NetUserId ChannelId;

            public AHelpState(BwoinkSystem parent, NetUserId channelId)
            {
                _parent = parent;
                ChannelId = channelId;
            }

            public BwoinkPanel? Panel = default;
            public int Unread = 0;
            public BwoinkTextMessage? LastMessage = default;
            public readonly Dictionary<Guid, BwoinkTextMessage> History = new();

            public void RequestLog() => _parent.RequestLog(ChannelId, LastMessage?.MessageId);

            public void CreatePanel()
            {
                if (Panel is not null)
                    return;

                Panel = new BwoinkPanel(this)
                {
                    Visible = false
                };

                LoadHistoryToPanel();
            }

            public void Associate()
            {
                if (_parent.IsAdmin())
                {
                    _parent.CreateAdminWindow();
                    var aw = _parent._adminWindow!;
                    if (!aw.HasPanel(Panel!))
                        aw.AddPanel(Panel!);
                }
                else
                {
                   _parent.CreateWindow();
                   var pw = _parent._plainWindow!;
                   Panel!.Visible = true;
                   if (pw.Contents.ChildCount < 1 || pw.Contents.GetChild(0) is not BwoinkPanel)
                       pw.Contents.AddChild(Panel!);
                }
            }

            public void Disassociate() => Panel?.Parent?.RemoveChild(Panel);

            public void Open()
            {
                CreatePanel();
                Associate();

                if (_parent.IsAdmin())
                {
                    var aw = _parent._adminWindow!;
                    aw.HideAllBut(Panel!);
                    aw.Open();
                }
                else
                {
                   _parent._plainWindow!.Open();
                }
            }

            public void MarkRead()
            {
                _parent.SendRead(ChannelId);
                Unread = 0;

                foreach (var m in History)
                    m.Value.Status = Status.Read;
            }

            public void Send(string text) => _parent.Send(ChannelId, text);

            public void ReceiveLine(BwoinkTextMessage btm)
            {
                if (btm.Status != Status.Read)
                {
                    Unread++;
                    LastMessage = btm;
                }

                if (Panel is not null)
                    Panel.ReceiveMessage(btm);
            }

            public void LoadHistoryToPanel()
            {
                if (Panel is null)
                    return;

                // TODO: Do this better. InsertAt?
                Panel.ClearMessages();

                foreach (var m in History.OrderBy(kvp => kvp.Key))
                    Panel.ReceiveMessage(m.Value);
            }
        }
    }
}
