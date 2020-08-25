using System;
using System.Collections.Generic;
using Content.Client.Interfaces;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client
{
    public class ClientNotifyManager : SharedNotifyManager, IClientNotifyManager
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private readonly List<PopupLabel> _aliveLabels = new List<PopupLabel>();
        private bool _initialized;

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _netManager.RegisterNetMessage<MsgDoNotifyCursor>(nameof(MsgDoNotifyCursor), DoNotifyCursor);
            _netManager.RegisterNetMessage<MsgDoNotifyCoordinates>(nameof(MsgDoNotifyCoordinates), DoNotifyCoordinates);
            _netManager.RegisterNetMessage<MsgDoNotifyEntity>(nameof(MsgDoNotifyEntity), DoNotifyEntity);

            _initialized = true;
        }

        private void DoNotifyCursor(MsgDoNotifyCursor message)
        {
            PopupMessage(message.Message);
        }

        private void DoNotifyCoordinates(MsgDoNotifyCoordinates message)
        {
            PopupMessage(_eyeManager.WorldToScreen(message.Coordinates), message.Message);
        }

        private void DoNotifyEntity(MsgDoNotifyEntity message)
        {
            if (!_entityManager.TryGetEntity(message.Entity, out var entity))
            {
                return;
            }

            PopupMessage(_eyeManager.WorldToScreen(entity.Transform.GridPosition), message.Message);
        }

        public override void PopupMessage(IEntity source, IEntity viewer, string message)
        {
            if (viewer != _playerManager.LocalPlayer.ControlledEntity)
            {
                return;
            }

            PopupMessage(_eyeManager.WorldToScreen(source.Transform.GridPosition), message);
        }

        public override void PopupMessage(GridCoordinates coordinates, IEntity viewer, string message)
        {
            if (viewer != _playerManager.LocalPlayer.ControlledEntity)
            {
                return;
            }

            PopupMessage(_eyeManager.WorldToScreen(coordinates), message);
        }

        public override void PopupMessageCursor(IEntity viewer, string message)
        {
            if (viewer != _playerManager.LocalPlayer.ControlledEntity)
            {
                return;
            }

            PopupMessage(message);
        }

        public void PopupMessage(ScreenCoordinates coordinates, string message)
        {
            var label = new PopupLabel
            {
                Text = message,
                StyleClasses = { StyleNano.StyleClassPopupMessage },
            };
            _userInterfaceManager.PopupRoot.AddChild(label);
            var minimumSize = label.CombinedMinimumSize;
            LayoutContainer.SetPosition(label, label.InitialPos = coordinates.Position - minimumSize / 2);
            _aliveLabels.Add(label);
        }

        public void PopupMessage(string message)
        {
            PopupMessage(new ScreenCoordinates(_inputManager.MouseScreenPosition), message);
        }

        public void FrameUpdate(FrameEventArgs eventArgs)
        {
            _aliveLabels.ForEach(l =>
            {
                if (l.TimeLeft > 3f)
                {
                    l.Dispose();
                }
            });

            _aliveLabels.RemoveAll(l => l.Disposed);
        }

        private class PopupLabel : Label
        {
            public float TimeLeft { get; private set; }
            public Vector2 InitialPos { get; set; }

            public PopupLabel()
            {
                ShadowOffsetXOverride = 1;
                ShadowOffsetYOverride = 1;
                FontColorShadowOverride = Color.Black;
            }

            protected override void Update(FrameEventArgs eventArgs)
            {
                TimeLeft += eventArgs.DeltaSeconds;
                LayoutContainer.SetPosition(this, InitialPos - (0, 20 * (TimeLeft * TimeLeft + TimeLeft)));
                if (TimeLeft > 0.5f)
                {
                    Modulate = Color.White.WithAlpha(1f - 0.2f * (float)Math.Pow(TimeLeft - 0.5f, 3f));
                }
            }
        }
    }

    public class PopupMessageCommand : IConsoleCommand
    {
        public string Command => "popupmsg";
        public string Description => "";
        public string Help => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var arg = args[0];
            var mgr = IoCManager.Resolve<IClientNotifyManager>();
            mgr.PopupMessage(arg);
            return false;
        }
    }
}
