using System;
using System.Collections.Generic;
using Content.Client.Interfaces;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client
{
    public class ClientNotifyManager : SharedNotifyManager, IClientNotifyManager
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private readonly List<PopupLabel> _aliveLabels = new();
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
            PopupMessage(_eyeManager.CoordinatesToScreen(message.Coordinates), message.Message);
        }

        private void DoNotifyEntity(MsgDoNotifyEntity message)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity == null ||
                !_entityManager.TryGetEntity(message.Entity, out var entity))
            {
                return;
            }

            PopupMessage(entity, _playerManager.LocalPlayer.ControlledEntity, message.Message);
        }

        public override void PopupMessage(IEntity source, IEntity viewer, string message)
        {
            PopupMessage(_eyeManager.CoordinatesToScreen(source.Transform.Coordinates), message, source);
        }

        public override void PopupMessage(EntityCoordinates coordinates, IEntity viewer, string message)
        {
            if (viewer != _playerManager.LocalPlayer?.ControlledEntity)
            {
                return;
            }

            PopupMessage(_eyeManager.CoordinatesToScreen(coordinates), message);
        }

        public override void PopupMessageCursor(IEntity viewer, string message)
        {
            if (viewer != _playerManager.LocalPlayer?.ControlledEntity)
            {
                return;
            }

            PopupMessage(message);
        }

        public void PopupMessage(ScreenCoordinates coordinates, string message)
        {
            PopupMessage(coordinates, message, null);
        }

        public void PopupMessage(ScreenCoordinates coordinates, string message, IEntity? entity)
        {
            var label = new PopupLabel(_eyeManager)
            {
                Entity = entity,
                Text = message,
                StyleClasses = { StyleNano.StyleClassPopupMessage },
            };

            _userInterfaceManager.PopupRoot.AddChild(label);
            label.Measure(Vector2.Infinity);
            var minimumSize = label.DesiredSize;

            label.InitialPos = (coordinates.Position / label.UIScale) - minimumSize / 2;
            LayoutContainer.SetPosition(label, label.InitialPos);
            _aliveLabels.Add(label);
        }

        public void PopupMessage(string message)
        {
            PopupMessage(_userInterfaceManager.MousePositionScaled, message);
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
            private readonly IEyeManager _eyeManager;

            public float TimeLeft { get; private set; }
            public Vector2 InitialPos { get; set; }
            public IEntity? Entity { get; set; }

            public PopupLabel(IEyeManager eyeManager)
            {
                _eyeManager = eyeManager;
                ShadowOffsetXOverride = 1;
                ShadowOffsetYOverride = 1;
                FontColorShadowOverride = Color.Black;
            }

            protected override void FrameUpdate(FrameEventArgs eventArgs)
            {
                TimeLeft += eventArgs.DeltaSeconds;

                var position = Entity == null
                    ? InitialPos
                    : (_eyeManager.CoordinatesToScreen(Entity.Transform.Coordinates).Position / UIScale) - DesiredSize / 2;

                LayoutContainer.SetPosition(this, position - (0, 20 * (TimeLeft * TimeLeft + TimeLeft)));

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

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var arg = args[0];
            var mgr = IoCManager.Resolve<IClientNotifyManager>();
            mgr.PopupMessage(arg);
        }
    }
}
