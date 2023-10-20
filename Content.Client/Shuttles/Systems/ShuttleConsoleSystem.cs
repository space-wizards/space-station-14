using Content.Shared.Input;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.Shuttles.Systems
{
    public sealed class ShuttleConsoleSystem : SharedShuttleConsoleSystem
    {
        [Dependency] private readonly IInputManager _input = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PilotComponent, ComponentHandleState>(OnHandleState);
            var shuttle = _input.Contexts.New("shuttle", "common");
            shuttle.AddFunction(ContentKeyFunctions.ShuttleStrafeUp);
            shuttle.AddFunction(ContentKeyFunctions.ShuttleStrafeDown);
            shuttle.AddFunction(ContentKeyFunctions.ShuttleStrafeLeft);
            shuttle.AddFunction(ContentKeyFunctions.ShuttleStrafeRight);
            shuttle.AddFunction(ContentKeyFunctions.ShuttleRotateLeft);
            shuttle.AddFunction(ContentKeyFunctions.ShuttleRotateRight);
            shuttle.AddFunction(ContentKeyFunctions.ShuttleBrake);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _input.Contexts.Remove("shuttle");
        }

        protected override void HandlePilotShutdown(EntityUid uid, PilotComponent component, ComponentShutdown args)
        {
            base.HandlePilotShutdown(uid, component, args);
            if (_playerManager.LocalPlayer?.ControlledEntity != uid) return;

            _input.Contexts.SetActiveContext("human");
        }

        private void OnHandleState(EntityUid uid, PilotComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not PilotComponentState state) return;

            var console = EnsureEntity<PilotComponent>(state.Console, uid);

            if (console == null)
            {
                component.Console = null;
                _input.Contexts.SetActiveContext("human");
                return;
            }

            if (!HasComp<ShuttleConsoleComponent>(console))
            {
                Log.Warning($"Unable to set Helmsman console to {console}");
                return;
            }

            component.Console = console;
            ActionBlockerSystem.UpdateCanMove(uid);
            _input.Contexts.SetActiveContext("shuttle");
        }
    }
}
