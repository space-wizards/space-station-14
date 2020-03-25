using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using static Content.Client.StaticIoC;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        private const float AttackTimeThreshold = 0.15f;

#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        private InputSystem _inputSystem;

        public bool UseOrAttackIsDown { get; private set; }
        private float _timeHeld;

        public override void Initialize()
        {
            base.Initialize();

            _gameHud.OnCombatModeChanged = OnCombatModeChanged;
            _gameHud.OnTargetingZoneChanged = OnTargetingZoneChanged;

            _inputSystem = EntitySystemManager.GetEntitySystem<InputSystem>();
            _inputSystem.BindMap.BindFunction(ContentKeyFunctions.UseOrAttack, new InputHandler(this));
            _inputSystem.BindMap.BindFunction(ContentKeyFunctions.ToggleCombatMode,
                InputCmdHandler.FromDelegate(CombatModeToggled));
            _overlayManager.AddOverlay(new CombatModeOverlay(this));
        }

        private void CombatModeToggled(ICommonSession session)
        {
            if (_gameTiming.IsFirstTimePredicted)
            {
                EntityManager.RaisePredictiveEvent(
                    new CombatModeSystemMessages.SetCombatModeActiveMessage(!IsInCombatMode()));

                // Just in case.
                UseOrAttackIsDown = false;
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _overlayManager.RemoveOverlay(nameof(CombatModeOverlay));
        }

        private bool IsInCombatMode()
        {
            var entity = _playerManager.LocalPlayer.ControlledEntity;
            if (entity == null || !entity.TryGetComponent(out CombatModeComponent combatMode))
            {
                return false;
            }

            return combatMode.IsInCombatMode;
        }

        private void OnTargetingZoneChanged(TargetingZone obj)
        {
            EntityManager.RaisePredictiveEvent(new CombatModeSystemMessages.SetTargetZoneMessage(obj));
        }

        private void OnCombatModeChanged(bool obj)
        {
            EntityManager.RaisePredictiveEvent(new CombatModeSystemMessages.SetCombatModeActiveMessage(obj));

            // Just in case.
            UseOrAttackIsDown = false;
        }

        private bool HandleInputMessage(ICommonSession session, InputCmdMessage message)
        {
            if (!(message is FullInputCmdMessage msg))
                return false;

            void SendMsg(BoundKeyFunction function, BoundKeyState state)
            {
                var functionId = _inputManager.NetworkBindMap.KeyFunctionID(function);

                var sendMsg = new FullInputCmdMessage(msg.Tick, functionId, state,
                    msg.Coordinates, msg.ScreenCoordinates, msg.Uid);
                _inputSystem.HandleInputCommand(session, function, sendMsg);
            }

            // If we are not in combat mode, relay it as a regular Use instead.
            if (!IsInCombatMode())
            {
                SendMsg(EngineKeyFunctions.Use, msg.State);
                return true;
            }

            if (msg.State == BoundKeyState.Down)
            {
                UseOrAttackIsDown = true;
                _timeHeld = 0;
                return true;
            }

            // Up.
            if (UseOrAttackIsDown && _timeHeld >= AttackTimeThreshold)
            {
                // Attack.
                SendMsg(ContentKeyFunctions.Attack, BoundKeyState.Down);
                SendMsg(ContentKeyFunctions.Attack, BoundKeyState.Up);
            }
            else
            {
                // Use.
                SendMsg(EngineKeyFunctions.Use, BoundKeyState.Down);
                SendMsg(EngineKeyFunctions.Use, BoundKeyState.Up);
            }

            UseOrAttackIsDown = false;

            return true;
        }

        public override void FrameUpdate(float frameTime)
        {
            if (UseOrAttackIsDown)
            {
                _timeHeld += frameTime;
            }
        }

        // Custom input handler type so we get the ENTIRE InputCmdMessage.
        private sealed class InputHandler : InputCmdHandler
        {
            private readonly CombatModeSystem _combatModeSystem;

            public InputHandler(CombatModeSystem combatModeSystem)
            {
                _combatModeSystem = combatModeSystem;
            }

            public override bool HandleCmdMessage(ICommonSession session, InputCmdMessage message)
            {
                return _combatModeSystem.HandleInputMessage(session, message);
            }
        }

        private sealed class CombatModeOverlay : Overlay
        {
            private readonly CombatModeSystem _system;

            public CombatModeOverlay(CombatModeSystem system) : base(nameof(CombatModeOverlay))
            {
                _system = system;
            }

            protected override void Draw(DrawingHandleBase handle)
            {
                var screenHandle = (DrawingHandleScreen) handle;

                var mousePos = IoCManager.Resolve<IInputManager>().MouseScreenPosition;

                if (_system.UseOrAttackIsDown && _system._timeHeld > AttackTimeThreshold)
                {
                    var tex = ResC.GetTexture($"/Textures/Objects/Tools/toolbox_r.png");

                    screenHandle.DrawTextureRect(tex, UIBox2.FromDimensions(mousePos, tex.Size * 2));
                }
            }
        }
    }
}
