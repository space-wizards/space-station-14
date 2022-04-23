using Content.Server.Singularity.Components;
using Content.Shared.Interaction;
using Content.Shared.Singularity.Components;
using Content.Server.Popups;
using Robust.Shared.Timing;
using Robust.Shared.Player;

namespace Content.Server.Singularity.EntitySystems
{
    public sealed class RadiationCollectorSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RadiationCollectorComponent, InteractHandEvent>(OnInteractHand);
        }

        private void OnInteractHand(EntityUid uid, RadiationCollectorComponent component, InteractHandEvent args)
        {
            var curTime = _gameTiming.CurTime;

            if(curTime < component.CoolDownEnd)
                return;

            if (!component.Enabled)
            {
                _popupSystem.PopupEntity(Loc.GetString("radiation-collector-component-use-on"), uid, Filter.Pvs(args.User));
                component.Collecting = true;
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("radiation-collector-component-use-off"), uid, Filter.Pvs(args.User));
                component.Collecting = false;
            }

            component.CoolDownEnd = curTime + TimeSpan.FromSeconds(0.81f);
        }
    }
}
