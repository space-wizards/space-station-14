using Content.Shared.Hands;
using Robust.Shared.GameObjects;

namespace Content.Server.Weapon.Ranged
{
    public sealed class RangedWeaponSysten : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ServerRangedWeaponComponent, HandSelectedEvent>(OnHandSelected);
        }

        private void OnHandSelected(EntityUid uid, ServerRangedWeaponComponent component, HandSelectedEvent args)
        {
            // Instead of dirtying on hand-select this component should probably by dirtied whenever it needs to be.
            // I take no responsibility for this code. It was like this when I got here.

            component.Dirty();
        }
    }
}
