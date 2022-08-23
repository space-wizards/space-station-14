using Content.Client.Stylesheets;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.Popups
{
    public sealed class PopupSystem : SharedPopupSystem
    {
        [Dependency] private readonly IInputManager _inputManager = default!;

        public IReadOnlyList<WorldPopupLabel> WorldLabels => _aliveWorldLabels;
        public IReadOnlyList<CursorPopupLabel> CursorLabels => _aliveCursorLabels;

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

        public void PopupCursor(string message, PopupType type = PopupType.Small)
        {
            var label = new CursorPopupLabel(_inputManager.MouseScreenPosition)
            {
                Text = message,
                Type = type,
            };

            _aliveCursorLabels.Add(label);
        }

        public void PopupCoordinates(string message, EntityCoordinates coordinates, PopupType type=PopupType.Small)
        {
            // Even if it's not in our map still get it (e.g. different viewports).
            PopupMessage(message, type, coordinates);
        }

        public void PopupEntity(string message, EntityUid uid, PopupType type=PopupType.Small)
        {
            if (!TryComp<TransformComponent>(uid, out var transform))
                return;

            PopupMessage(message, type, transform.Coordinates);
        }

        private void PopupMessage(string message, PopupType type, EntityCoordinates coordinates)
        {
            var label = new WorldPopupLabel(coordinates)
            {
                Text = message,
                Type = type,
            };

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
            _aliveCursorLabels.Clear();
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

            for (var i = 0; i < _aliveWorldLabels.Count; i++)
            {
                var label = _aliveWorldLabels[i];
                if (label.TotalTime > PopupLifetime || Deleted(label.InitialPos.EntityId))
                {
                    _aliveWorldLabels.RemoveSwap(i);
                    i--;
                    continue;
                }

                label.TotalTime += frameTime;
            }

            for (var i = 0; i < _aliveCursorLabels.Count; i++)
            {
                var label = _aliveCursorLabels[i];
                label.TotalTime += frameTime;

                if (label.TotalTime > PopupLifetime)
                {
                    _aliveCursorLabels.RemoveSwap(i);
                    i--;
                }
            }
        }

        public abstract class PopupLabel
        {
            public PopupType Type = PopupType.Small;
            public string Text { get; set; } = string.Empty;
            public float TotalTime { get; set; }
        }

        public sealed class CursorPopupLabel : PopupLabel
        {
            public ScreenCoordinates InitialPos { get; set; }

            public CursorPopupLabel(ScreenCoordinates screenCoords) : base()
            {
                InitialPos = screenCoords;
            }
        }

        public sealed class WorldPopupLabel : PopupLabel
        {
            /// <summary>
            /// The original EntityCoordinates of the label.
            /// </summary>
            public EntityCoordinates InitialPos { get; set; }

            public WorldPopupLabel(EntityCoordinates coordinates)
            {
                InitialPos = coordinates;
            }
        }
    }
}
