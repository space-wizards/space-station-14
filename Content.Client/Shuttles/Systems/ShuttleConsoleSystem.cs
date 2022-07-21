using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;

namespace Content.Client.Shuttles.Systems
{
    public sealed class ShuttleConsoleSystem : SharedShuttleConsoleSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PilotComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandleState(EntityUid uid, PilotComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not PilotComponentState state) return;

            var console = state.Console.GetValueOrDefault();
            if (!console.IsValid())
            {
                component.Console = null;
                return;
            }

            if (!TryComp<ShuttleConsoleComponent>(console, out var shuttleConsoleComponent))
            {
                Logger.Warning($"Unable to set Helmsman console to {console}");
                return;
            }

            component.Console = shuttleConsoleComponent;
            ActionBlockerSystem.UpdateCanMove(uid);
        }
    }
}
