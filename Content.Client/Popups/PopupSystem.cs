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

        private readonly List<PopupLabel> _aliveLabels = new();

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
            PopupMessage(message, _inputManager.MouseScreenPosition);
        }

        public void PopupCoordinates(string message, EntityCoordinates coordinates)
        {
            var mapCoords = coordinates.ToMap(EntityManager);
            if (_eyeManager.CurrentMap != mapCoords.MapId)
                return;
            PopupMessage(message, _eyeManager.MapToScreen(mapCoords));
        }

        public void PopupEntity(string message, EntityUid uid)
        {
            if (!EntityManager.EntityExists(uid))
                return;

            var transform = EntityManager.GetComponent<TransformComponent>(uid);
            if (_eyeManager.CurrentMap != transform.MapID)
                return; // TODO: entity may be outside of PVS, but enter PVS at a later time. So the pop-up should still get tracked?

            PopupMessage(message, _eyeManager.CoordinatesToScreen(transform.Coordinates), uid);
        }

        private void PopupMessage(string message, ScreenCoordinates coordinates, EntityUid? entity = null)
        {
            var mapCoords = _eyeManager.ScreenToMap(coordinates);
            PopupMessage(message, EntityCoordinates.FromMap(_map, mapCoords), entity);
        }

        private void PopupMessage(string message, EntityCoordinates coordinates, EntityUid? entity = null)
        {
            var label = new PopupLabel(_eyeManager, EntityManager)
            {
                Entity = entity,
                Text = message,
                StyleClasses = { StyleNano.StyleClassPopupMessage },
            };

            _userInterfaceManager.PopupRoot.AddChild(label);
            label.Measure(Vector2.Infinity);

            label.InitialPos = coordinates;
            LayoutContainer.SetPosition(label, label.InitialPos.Position);
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
            if (_aliveLabels.Count == 0) return;

            var player = _playerManager.LocalPlayer?.ControlledEntity;
            var playerPos = player != null ? Transform(player.Value).MapPosition : MapCoordinates.Nullspace;

            // ReSharper disable once ConvertToLocalFunction
            var predicate = static (EntityUid uid, (EntityUid? compOwner, EntityUid? attachedEntity) data)
                => uid == data.compOwner || uid == data.attachedEntity;
            var occluded = player != null && _examineSystem.IsOccluded(player.Value);

            for (var i = _aliveLabels.Count - 1; i >= 0; i--)
            {
                var label = _aliveLabels[i];
                if (label.TotalTime > PopupLifetime ||
                    label.Entity != null && Deleted(label.Entity))
                {
                    label.Dispose();
                    _aliveLabels.RemoveAt(i);
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
        }

        private sealed class PopupLabel : Label
        {
            private readonly IEyeManager _eyeManager;
            private readonly IEntityManager _entityManager;

            public float TotalTime { get; private set; }
            /// <summary>
            /// The original Mapid and ScreenPosition of the label.
            /// </summary>
            /// <remarks>
            /// Yes that's right it's not technically MapCoordinates.
            /// </remarks>
            public EntityCoordinates InitialPos { get; set; }
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
                TotalTime += eventArgs.DeltaSeconds;

                ScreenCoordinates screenCoords;

                if (Entity == null)
                    screenCoords = _eyeManager.CoordinatesToScreen(InitialPos);
                else if (_entityManager.TryGetComponent(Entity.Value, out TransformComponent xform))
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

                if (TotalTime > 0.5f)
                {
                    Modulate = Color.White.WithAlpha(1f - 0.2f * (float)Math.Pow(TotalTime - 0.5f, 3f));
                }
            }
        }
    }
}
