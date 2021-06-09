using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    public class HypospraySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HyposprayComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<HyposprayComponent, ClickAttackEvent>(OnClickAttack);
        }

        public void OnAfterInteract(EntityUid uid, HyposprayComponent comp, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;
            var target = args.Target;
            var user = args.User;

            comp.TryDoInject(target, user);
        }

        public void OnClickAttack(EntityUid uid, HyposprayComponent comp, ClickAttackEvent args)
        {
            var target = args.TargetEntity;
            var user = args.User;

            comp.TryDoInject(target, user);
        }
    }
}
