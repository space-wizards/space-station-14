using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Content.Server.Stunnable.Components;
using Content.Shared.Stunnable;
using Content.Server.Polymorph.Components;

namespace Content.Server.Stunnable
{
    public sealed class StunOnTouchSystem : SharedStunOnTouchSystem
    {
        [Dependency] private readonly StunSystem _stunSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunOnTouchComponent, StartCollideEvent>(OnCollide);
        }

        private void OnCollide(EntityUid uid, StunOnTouchComponent component, ref StartCollideEvent args)
        {
            if(TryComp<PolymorphedEntityComponent>(uid, out var polymoprhed) && polymoprhed.Parent == args.OtherEntity)
                return;

            var ent = args.OtherEntity;
            _stunSystem.TryParalyze(ent, TimeSpan.FromSeconds(component.StunTime), false);
            
        }

        
    }
}