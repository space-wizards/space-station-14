using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems.Weapon.Melee;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    public class HypospraySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HyposprayComponent, ClickAttackEvent>(OnClickAttack);
        }

        public void OnClickAttack(EntityUid uid, HyposprayComponent comp, ClickAttackEvent args)
        {
            var target = args.TargetEntity;
            var user = args.User;

            comp.TryDoInject(target, user);
        }
    }
}
