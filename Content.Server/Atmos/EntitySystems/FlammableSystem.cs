using Content.Server.Atmos.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Atmos.EntitySystems
{
    internal sealed class FlammableSystem : EntitySystem
    {
        // TODO: Port the rest of Flammable.
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FlammableComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, FlammableComponent component, StartCollideEvent args)
        {
            if (!args.OtherFixture.Body.Owner.TryGetComponent(out FlammableComponent? otherFlammable))
                return;

            if (!component.FireSpread || !otherFlammable.FireSpread)
                return;

            if (component.OnFire)
            {
                if (otherFlammable.OnFire)
                {
                    var fireSplit = (component.FireStacks + otherFlammable.FireStacks) / 2;
                    component.FireStacks = fireSplit;
                    otherFlammable.FireStacks = fireSplit;
                }
                else
                {
                    component.FireStacks /= 2;
                    otherFlammable.FireStacks += component.FireStacks;
                    Ignite(otherFlammable);
                }
            } else if (otherFlammable.OnFire)
            {
                otherFlammable.FireStacks /= 2;
                component.FireStacks += otherFlammable.FireStacks;
                Ignite(component);
            }
        }

        internal void Ignite(FlammableComponent component)
        {
            if (component.FireStacks > 0 && !component.OnFire)
            {
                component.OnFire = true;
            }

            component.UpdateAppearance();
        }
    }
}
