using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Popups
{
    public sealed class PopupSystem : SharedPopupSystem
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IOverlayManager _overlay = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IResourceCache _resource = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

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
            _overlay
                .AddOverlay(new PopupOverlay(_configManager, EntityManager, _playerManager, _prototype, _resource, _uiManager, this));
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _overlay
                .RemoveOverlay<PopupOverlay>();
        }

        private void PopupMessage(string message, PopupType type, EntityCoordinates coordinates, EntityUid? entity = null)
        {
            var label = new WorldPopupLabel(coordinates)
            {
                Text = message,
                Type = type,
            };

            _aliveWorldLabels.Add(label);
        }

        #region Abstract Method Implementations
        public override void PopupCoordinates(string message, EntityCoordinates coordinates, PopupType type = PopupType.Small)
        {
            PopupMessage(message, type, coordinates, null);
        }

        public override void PopupCoordinates(string message, EntityCoordinates coordinates, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            if (_playerManager.LocalPlayer?.Session == recipient)
                PopupMessage(message, type, coordinates, null);
        }

        public override void PopupCoordinates(string message, EntityCoordinates coordinates, EntityUid recipient, PopupType type = PopupType.Small)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity == recipient)
                PopupMessage(message, type, coordinates, null);
        }

        public override void PopupCursor(string message, PopupType type = PopupType.Small)
        {
            var label = new CursorPopupLabel(_inputManager.MouseScreenPosition)
            {
                Text = message,
                Type = type,
            };

            _aliveCursorLabels.Add(label);
        }

        public override void PopupCursor(string message, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            if (_playerManager.LocalPlayer?.Session == recipient)
                PopupCursor(message, type);
        }

        public override void PopupCursor(string message, EntityUid recipient, PopupType type = PopupType.Small)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity == recipient)
                PopupCursor(message, type);
        }

        public override void PopupCoordinates(string message, EntityCoordinates coordinates, Filter filter, bool replayRecord, PopupType type = PopupType.Small)
        {
            PopupCoordinates(message, coordinates, type);
        }

        public override void PopupEntity(string message, EntityUid uid, EntityUid recipient, PopupType type = PopupType.Small)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity == recipient)
                PopupEntity(message, uid, type);
        }

        public override void PopupEntity(string message, EntityUid uid, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            if (_playerManager.LocalPlayer?.Session == recipient)
                PopupEntity(message, uid, type);
        }

        public override void PopupEntity(string message, EntityUid uid, Filter filter, bool recordReplay, PopupType type=PopupType.Small)
        {
            PopupEntity(message, uid, type);
        }

        public override void PopupClient(string message, EntityUid uid, EntityUid recipient, PopupType type = PopupType.Small)
        {
            if (_timing.IsFirstTimePredicted)
                PopupEntity(message, uid, recipient);
        }

        public override void PopupEntity(string message, EntityUid uid, PopupType type = PopupType.Small)
        {
            if (!EntityManager.EntityExists(uid))
                return;

            var transform = EntityManager.GetComponent<TransformComponent>(uid);

            PopupMessage(message, type, transform.Coordinates, uid);
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

        public override void FrameUpdate(float frameTime)
        {
            if (_aliveWorldLabels.Count == 0 && _aliveCursorLabels.Count == 0)
                return;

            for (var i = 0; i < _aliveWorldLabels.Count; i++)
            {
                var label = _aliveWorldLabels[i];
                label.TotalTime += frameTime;

                if (label.TotalTime > PopupLifetime || Deleted(label.InitialPos.EntityId))
                {
                    _aliveWorldLabels.RemoveSwap(i);
                    i--;
                }
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
            public ScreenCoordinates InitialPos;

            public CursorPopupLabel(ScreenCoordinates screenCoords)
            {
                InitialPos = screenCoords;
            }
        }

        public sealed class WorldPopupLabel : PopupLabel
        {
            /// <summary>
            /// The original EntityCoordinates of the label.
            /// </summary>
            public EntityCoordinates InitialPos;

            public WorldPopupLabel(EntityCoordinates coordinates)
            {
                InitialPos = coordinates;
            }
        }
    }
}
