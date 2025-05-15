using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Content.Server.Stunnable.Components;
using Content.Shared.Stunnable;

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
            var ent = args.OtherEntity;
            _stunSystem.TryKnockdown(ent, TimeSpan.FromSeconds(component.StunTime), false);
            
        }

        
    }
}