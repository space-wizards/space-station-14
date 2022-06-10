using Content.Client.Stylesheets;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Popups
{
    public sealed class PopupSystem : SharedPopupSystem
    {
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

        private readonly List<WorldPopupLabel> _aliveWorldLabels = new();
        private readonly List<CursorPopupLabel> _aliveCursorLabels = new();

        public const float PopupLifetime = 3f;

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
            var label = new CursorPopupLabel(_inputManager.MouseScreenPosition)
            {
                Text = message,
                StyleClasses = { StyleNano.StyleClassPopupMessage },
            };
            _userInterfaceManager.PopupRoot.AddChild(label);
            _aliveCursorLabels.Add(label);
        }

        public void PopupCoordinates(string message, EntityCoordinates coordinates)
        {
            if (_eyeManager.CurrentMap != Transform(coordinates.EntityId).MapID)
                return;
            PopupMessage(message, coordinates, null);
        }

        public void PopupEntity(string message, EntityUid uid)
        {
            if (!EntityManager.EntityExists(uid))
                return;

            var transform = EntityManager.GetComponent<TransformComponent>(uid);
            if (_eyeManager.CurrentMap != transform.MapID)
                return; // TODO: entity may be outside of PVS, but enter PVS at a later time. So the pop-up should still get tracked?

            PopupMessage(message, transform.Coordinates, uid);
        }

        private void PopupMessage(string message, EntityCoordinates coordinates, EntityUid? entity = null)
        {
            var label = new WorldPopupLabel(_eyeManager, EntityManager)
            {
                Entity = entity,
                Text = message,
                StyleClasses = { StyleNano.StyleClassPopupMessage },
            };

            _userInterfaceManager.PopupRoot.AddChild(label);
            label.Measure(Vector2.Infinity);

            label.InitialPos = coordinates;
            _aliveWorldLabels.Add(label);
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
            foreach (var label in _aliveWorldLabels)
            {
                label.Dispose();
            }

            _aliveWorldLabels.Clear();
        }

        #endregion

        public override void FrameUpdate(float frameTime)
        {
            if (_aliveWorldLabels.Count == 0) return;

            var player = _playerManager.LocalPlayer?.ControlledEntity;
            var playerPos = player != null ? Transform(player.Value).MapPosition : MapCoordinates.Nullspace;

            // ReSharper disable once ConvertToLocalFunction
            var predicate = static (EntityUid uid, (EntityUid? compOwner, EntityUid? attachedEntity) data)
                => uid == data.compOwner || uid == data.attachedEntity;
            var occluded = player != null && _examineSystem.IsOccluded(player.Value);

            for (var i = _aliveWorldLabels.Count - 1; i >= 0; i--)
            {
                var label = _aliveWorldLabels[i];
                if (label.TotalTime > PopupLifetime ||
                    label.Entity != null && Deleted(label.Entity))
                {
                    label.Dispose();
                    _aliveWorldLabels.RemoveSwap(i);
                    continue;
                }

                if (label.Entity == player)
                {
                    label.Visible = true;
                    continue;
                }

                var otherPos = label.Entity != null ? Transform(label.Entity.Value).MapPosition : label.InitialPos.ToMap(EntityManager);

                if (occluded && !ExamineSystemShared.InRangeUnOccluded(
                        playerPos,
                        otherPos, 0f,
                        (label.Entity, player), predicate))
                {
                    label.Visible = false;
                    continue;
                }

                label.Visible = true;
            }

            for (var i = _aliveCursorLabels.Count - 1; i >= 0; i--)
            {
                var label = _aliveCursorLabels[i];
                if (label.TotalTime > PopupLifetime)
                {
                    label.Dispose();
                    _aliveCursorLabels.RemoveSwap(i);
                }
            }
        }

        private abstract class PopupLabel : Label
        {
            public float TotalTime { get; protected set; }

            public PopupLabel()
            {
                ShadowOffsetXOverride = ShadowOffsetYOverride = 1;
                FontColorShadowOverride = Color.Black;
                Measure(Vector2.Infinity);
            }

            protected override void FrameUpdate(FrameEventArgs eventArgs)
            {
                TotalTime += eventArgs.DeltaSeconds;
                if (TotalTime > 0.5f)
                    Modulate = Color.White.WithAlpha(1f - 0.2f * MathF.Pow(TotalTime - 0.5f, 3f));
            }
        }

        private sealed class CursorPopupLabel : PopupLabel
        {
            public Vector2 InitialPos { get; set; }

            public CursorPopupLabel(ScreenCoordinates screenCoords) : base()
            {
                InitialPos = screenCoords.Position / UIScale - DesiredSize / 2;
            }

            protected override void FrameUpdate(FrameEventArgs eventArgs)
            {
                base.FrameUpdate(eventArgs);
                LayoutContainer.SetPosition(this, InitialPos - (0, 20 * (TotalTime * TotalTime + TotalTime)));
            }
        }

        private sealed class WorldPopupLabel : PopupLabel
        {
            private readonly IEyeManager _eyeManager;
            private readonly IEntityManager _entityManager;

            /// <summary>
            /// The original Mapid and ScreenPosition of the label.
            /// </summary>
            /// <remarks>
            /// Yes that's right it's not technically MapCoordinates.
            /// </remarks>
            public EntityCoordinates InitialPos { get; set; }
            public EntityUid? Entity { get; set; }

            public WorldPopupLabel(IEyeManager eyeManager, IEntityManager entityManager) : base()
            {
                _eyeManager = eyeManager;
                _entityManager = entityManager;
            }

            protected override void FrameUpdate(FrameEventArgs eventArgs)
            {
                base.FrameUpdate(eventArgs);
                ScreenCoordinates screenCoords;

                if (Entity == null)
                    screenCoords = _eyeManager.CoordinatesToScreen(InitialPos);
                else if (_entityManager.TryGetComponent(Entity.Value, out TransformComponent? xform)
                    && xform.MapID == _eyeManager.CurrentMap)
                    screenCoords = _eyeManager.CoordinatesToScreen(xform.Coordinates);
                else
                {
                    Visible = false;
                    if (Entity != null && _entityManager.Deleted(Entity))
                        TotalTime += PopupLifetime;
                    return;
                }

                Visible = true;
                var position = screenCoords.Position / UIScale - DesiredSize / 2;
                LayoutContainer.SetPosition(this, position - (0, 20 * (TotalTime * TotalTime + TotalTime)));
            }
        }
    }
}
