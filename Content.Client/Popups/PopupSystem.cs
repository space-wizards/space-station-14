using System;
using System.Collections.Generic;
using Content.Client.Stylesheets;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.Popups
{
    public sealed class PopupSystem : SharedPopupSystem
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        private readonly List<PopupLabel> _aliveLabels = new();

        public override void Initialize()
        {
            SubscribeNetworkEvent<PopupCursorEvent>(OnPopupCursorEvent);
            SubscribeNetworkEvent<PopupCoordinatesEvent>(OnPopupCoordinatesEvent);
            SubscribeNetworkEvent<PopupEntityEvent>(OnPopupEntityEvent);
            SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        #region Actual Implementation

        public void PopupCursor(string message)
        {
            PopupMessage(message, _userInterfaceManager.MousePositionScaled);
        }

        public void PopupCoordinates(string message, EntityCoordinates coordinates)
        {
            PopupMessage(message, _eyeManager.CoordinatesToScreen(coordinates));
        }

        public void PopupEntity(string message, EntityUid uid)
        {
            if (!EntityManager.EntityExists(uid))
                return;

            var transform = EntityManager.GetComponent<TransformComponent>(uid);
            PopupMessage(message, _eyeManager.CoordinatesToScreen(transform.Coordinates));
        }

        public void PopupMessage(string message, ScreenCoordinates coordinates, EntityUid? entity = null)
        {
            var label = new PopupLabel(_eyeManager, EntityManager)
            {
                Entity = entity,
                Text = message,
                StyleClasses = { StyleNano.StyleClassPopupMessage },
            };

            _userInterfaceManager.PopupRoot.AddChild(label);
            label.Measure(Vector2.Infinity);
            var minimumSize = label.DesiredSize;

            label.InitialPos = coordinates.Position / label.UIScale - minimumSize / 2;
            LayoutContainer.SetPosition(label, label.InitialPos);
            _aliveLabels.Add(label);
        }

        #endregion

        #region Abstract Method Implementations

        public override void PopupCursor(string message, Filter filter)
        {
            if (!filter.CheckPrediction)
                return;

            PopupCursor(message);
        }

        public override void PopupCoordinates(string message, EntityCoordinates coordinates, Filter filter)
        {
            if (!filter.CheckPrediction)
                return;

            PopupCoordinates(message, coordinates);
        }

        public override void PopupEntity(string message, EntityUid uid, Filter filter)
        {
            if (!filter.CheckPrediction)
                return;

            PopupEntity(message, uid);
        }

        #endregion

        #region Network Event Handlers

        private void OnPopupCursorEvent(PopupCursorEvent ev)
        {
            PopupCursor(ev.Message);
        }

        private void OnPopupCoordinatesEvent(PopupCoordinatesEvent ev)
        {
            PopupCoordinates(ev.Message, ev.Coordinates);
        }

        private void OnPopupEntityEvent(PopupEntityEvent ev)
        {
            PopupEntity(ev.Message, ev.Uid);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent ev)
        {
            foreach (var label in _aliveLabels)
            {
                label.Dispose();
            }

            _aliveLabels.Clear();
        }

        #endregion

        public override void FrameUpdate(float frameTime)
        {
            foreach (var l in _aliveLabels)
            {
                if (l.TimeLeft > 3f)
                    l.Dispose();
            }

            _aliveLabels.RemoveAll(l => l.Disposed);
        }

        private sealed class PopupLabel : Label
        {
            private readonly IEyeManager _eyeManager;
            private readonly IEntityManager _entityManager;

            public float TimeLeft { get; private set; }
            public Vector2 InitialPos { get; set; }
            public EntityUid? Entity { get; set; }

            public PopupLabel(IEyeManager eyeManager, IEntityManager entityManager)
            {
                _eyeManager = eyeManager;
                _entityManager = entityManager;
                ShadowOffsetXOverride = 1;
                ShadowOffsetYOverride = 1;
                FontColorShadowOverride = Color.Black;
            }

            protected override void FrameUpdate(FrameEventArgs eventArgs)
            {
                TimeLeft += eventArgs.DeltaSeconds;

                var position = Entity == null
                    ? InitialPos
                    : (_eyeManager.CoordinatesToScreen(_entityManager.GetComponent<TransformComponent>(Entity.Value).Coordinates).Position / UIScale) - DesiredSize / 2;

                LayoutContainer.SetPosition(this, position - (0, 20 * (TimeLeft * TimeLeft + TimeLeft)));

                if (TimeLeft > 0.5f)
                {
                    Modulate = Color.White.WithAlpha(1f - 0.2f * (float)Math.Pow(TimeLeft - 0.5f, 3f));
                }
            }
        }
    }
}
