using Content.Server.Atmos.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasAnalyzerSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        }

        public override void Update(float frameTime)
        {
            foreach (var analyzer in EntityManager.EntityQuery<GasAnalyzerComponent>(true))
            {
                analyzer.Update(frameTime);
            }
        }

        private void OnAfterInteract(EntityUid uid, GasAnalyzerComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
            {
                _popup.PopupEntity(Loc.GetString("gas-analyzer-component-player-cannot-reach-message"), args.User, Filter.Entities(args.User));
                return;
            }

            if (TryComp(args.User, out ActorComponent? actor))
            {
                component.OpenInterface(actor.PlayerSession, args.ClickLocation);
            }

            args.Handled = true;
        }
    }
}
