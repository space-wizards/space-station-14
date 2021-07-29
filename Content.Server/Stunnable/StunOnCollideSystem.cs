using Content.Server.Stunnable.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Stunnable
{
    [UsedImplicitly]
    internal sealed class StunOnCollideSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StunOnCollideComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, StunOnCollideComponent component, StartCollideEvent args)
        {
            if (args.OtherFixture.Body.Owner.TryGetComponent(out StunnableComponent? stunnableComponent))
            {
                stunnableComponent.Stun(component.StunAmount);
                stunnableComponent.Knockdown(component.KnockdownAmount);
                stunnableComponent.Slowdown(component.SlowdownAmount);
            }
        }
    }
}
