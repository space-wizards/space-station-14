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

        public void PopupCursor(string message, PopupType type=PopupType.Small)
        {
            var label = new CursorPopupLabel(_inputManager.MouseScreenPosition)
            {
                Text = message,
                StyleClasses = { GetStyleClass(type) },
            };
            _userInterfaceManager.PopupRoot.AddChild(label);
            _aliveCursorLabels.Add(label);
        }

        public void PopupCoordinates(string message, EntityCoordinates coordinates, PopupType type=PopupType.Small)
        {
            if (_eyeManager.CurrentMap != Transform(coordinates.EntityId).MapID)
                return;
            PopupMessage(message, type, coordinates, null);
        }

        public void PopupEntity(string message, EntityUid uid, PopupType type=PopupType.Small)
        {
            if (!EntityManager.EntityExists(uid))
                return;

            var transform = EntityManager.GetComponent<TransformComponent>(uid);
            if (_eyeManager.CurrentMap != transform.MapID)
                return; // TODO: entity may be outside of PVS, but enter PVS at a later time. So the pop-up should still get tracked?

            PopupMessage(message, type, transform.Coordinates, uid);
        }

        private void PopupMessage(string message, PopupType type, EntityCoordinates coordinates, EntityUid? entity = null)
        {
            var label = new WorldPopupLabel(_eyeManager, EntityManager)
            {
                Entity = entity,
                Text = message,
                StyleClasses = { GetStyleClass(type) },
            };

            _userInterfaceManager.PopupRoot.AddChild(label);
            label.Measure(Vector2.Infinity);

            label.InitialPos = coordinates;
            _aliveWorldLabels.Add(label);
        }

        #endregion

        #region Abstract Method Implementations

        public override void PopupCursor(string message, Filter filter, PopupType type=PopupType.Small)
        {
            if (!filter.CheckPrediction)
                return;

            PopupCursor(message, type);
        }

        public override void PopupCoordinates(string message, EntityCoordinates coordinates, Filter filter, PopupType type=PopupType.Small)
        {
            if (!filter.CheckPrediction)
                return;

            PopupCoordinates(message, coordinates, type);
        }

        public override void PopupEntity(string message, EntityUid uid, Filter filter, PopupType type=PopupType.Small)
        {
            if (!filter.CheckPrediction)
                return;

            PopupEntity(message, uid, type);
        }

        #endregion

        #region Network Event Handlers

        private void OnPopupCursorEvent(PopupCursorEvent ev)
        {
            PopupCursor(ev.Message, ev.Type);
        }

        private void OnPopupCoordinatesEvent(PopupCoordinatesEvent ev)
        {
            PopupCoordinates(ev.Message, ev.Coordinates, ev.Type);
        }

        private void OnPopupEntityEvent(PopupEntityEvent ev)
        {
            PopupEntity(ev.Message, ev.Uid, ev.Type);
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

        private static string GetStyleClass(PopupType type) =>
            type switch
            {
                PopupType.Small => StyleNano.StyleClassPopupMessageSmall,
                PopupType.SmallCaution => StyleNano.StyleClassPopupMessageSmallCaution,
                PopupType.Medium => StyleNano.StyleClassPopupMessageMedium,
                PopupType.MediumCaution => StyleNano.StyleClassPopupMessageMediumCaution,
                PopupType.Large => StyleNano.StyleClassPopupMessageLarge,
                PopupType.LargeCaution => StyleNano.StyleClassPopupMessageLargeCaution,
                _ => StyleNano.StyleClassPopupMessageSmall
            };

        public override void FrameUpdate(float frameTime)
        {
            if (_aliveWorldLabels.Count == 0 && _aliveCursorLabels.Count == 0)
                return;

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
                InitialPos = screenCoords.Position - DesiredSize / 2;
            }

            protected override void FrameUpdate(FrameEventArgs eventArgs)
            {
                base.FrameUpdate(eventArgs);
                LayoutContainer.SetPosition(this, InitialPos / UIScale - (0, 20 * (TotalTime * TotalTime + TotalTime)));
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
