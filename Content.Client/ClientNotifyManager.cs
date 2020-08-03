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
using Robust.Client.UserInterface;
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
#pragma warning disable 649
        [Dependency] private IPlayerManager _playerManager;
        [Dependency] private IUserInterfaceManager _userInterfaceManager;
        [Dependency] private IInputManager _inputManager;
        [Dependency] private IEyeManager _eyeManager;
        [Dependency] private IClientNetManager _netManager;
        [Dependency] private IEntityManager _entityManager;
#pragma warning restore 649

        private readonly List<PopupLabel> _aliveLabels = new List<PopupLabel>();
        private bool _initialized;
        private Popup _tooltipOpen;

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _netManager.RegisterNetMessage<MsgDoNotifyCursor>(nameof(MsgDoNotifyCursor), DoNotifyCursor);
            _netManager.RegisterNetMessage<MsgDoNotifyCoordinates>(nameof(MsgDoNotifyCoordinates), DoNotifyCoordinates);
            _netManager.RegisterNetMessage<MsgDoNotifyEntity>(nameof(MsgDoNotifyEntity), DoNotifyEntity);
            _netManager.RegisterNetMessage<MsgDoNotifyTooltipCursor>(nameof(MsgDoNotifyTooltipCursor), DoNotifyTooltipCursor);
            _netManager.RegisterNetMessage<MsgDoNotifyTooltipCoordinates>(nameof(MsgDoNotifyTooltipCoordinates), DoNotifyTooltipCoordinates);
            _netManager.RegisterNetMessage<MsgDoNotifyTooltipEntity>(nameof(MsgDoNotifyTooltipEntity), DoNotifyTooltipEntity);

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

        private void DoNotifyTooltipCursor(MsgDoNotifyTooltipCursor message)
        {
            PopupTooltip(message.Title, message.Message);
        }

        private void DoNotifyTooltipCoordinates(MsgDoNotifyTooltipCoordinates message)
        {
            PopupTooltip(_eyeManager.WorldToScreen(message.Coordinates), message.Title, message.Message);
        }

        private void DoNotifyTooltipEntity(MsgDoNotifyTooltipEntity message)
        {
            if (!_entityManager.TryGetEntity(message.Entity, out var entity))
            {
                return;
            }

            PopupTooltip(_eyeManager.WorldToScreen(entity.Transform.GridPosition), message.Title, message.Message);
        }

        public override void PopupTooltip(IEntity source, IEntity viewer, string title, string message)
        {
            if (viewer != _playerManager.LocalPlayer.ControlledEntity)
            {
                return;
            }

            PopupTooltip(_eyeManager.WorldToScreen(source.Transform.GridPosition), title, message);
        }

        public override void PopupTooltip(GridCoordinates coordinates, IEntity viewer, string title, string message)
        {
            if (viewer != _playerManager.LocalPlayer.ControlledEntity)
            {
                return;
            }

            PopupTooltip(_eyeManager.WorldToScreen(coordinates), title, message);
        }

        public override void PopupTooltipCursor(IEntity viewer, string title, string message)
        {
            if (viewer != _playerManager.LocalPlayer.ControlledEntity)
            {
                return;
            }

            PopupTooltip(title, message);
        }

        public void CloseTooltip()
        {
            if (_tooltipOpen != null)
            {
                _tooltipOpen.Dispose();
                _tooltipOpen = null;
            }
        }

        //TODO: send this in the netmessage?
        public const string StyleClassEntityTooltip = "entity-tooltip";
        public void PopupTooltip(ScreenCoordinates coordinates, string title, string message)
        {
            CloseTooltip();
            _tooltipOpen = new Popup();
            _userInterfaceManager.ModalRoot.AddChild(_tooltipOpen);
            var panel = new PanelContainer();
            panel.AddStyleClass(StyleClassEntityTooltip);
            panel.ModulateSelfOverride = Color.LightGray.WithAlpha(0.90f);
            _tooltipOpen.AddChild(panel);
            var vBox = new VBoxContainer();
            panel.AddChild(vBox);
            var hBox = new HBoxContainer { SeparationOverride = 5 };
            vBox.AddChild(hBox);

            hBox.AddChild(new Label
            {
                Text = title,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
            });

            var richLabel = new RichTextLabel();
            richLabel.SetMessage(message);
            vBox.AddChild(richLabel);

            const float minWidth = 300;
            var size = Vector2.ComponentMax((minWidth, 0), panel.CombinedMinimumSize);
            _tooltipOpen.Open(UIBox2.FromDimensions(coordinates.Position, size));
        }

        public void PopupTooltip(string title, string message)
        {
            PopupTooltip(new ScreenCoordinates(_inputManager.MouseScreenPosition), title, message);
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
