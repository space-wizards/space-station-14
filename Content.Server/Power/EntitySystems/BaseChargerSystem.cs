using Content.Server.Power.Components;
using Content.Server.PowerCell.Components;
using Content.Server.Weapon;
using Content.Shared.ActionBlocker;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    internal sealed class BaseChargerSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PowerCellChargerComponent, GetAlternativeVerbsEvent>(AddEjectVerb);
            SubscribeLocalEvent<PowerCellChargerComponent, GetInteractionVerbsEvent>(AddInsertVerb);
            SubscribeLocalEvent<WeaponCapacitorChargerComponent, GetAlternativeVerbsEvent>(AddEjectVerb);
            SubscribeLocalEvent<WeaponCapacitorChargerComponent, GetInteractionVerbsEvent>(AddInsertVerb);
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<BaseCharger>())
            {
                comp.OnUpdate(frameTime);
            }
        }

        // TODO VERBS EJECTABLES Standardize eject/insert verbs into a single system?
        private void AddEjectVerb(EntityUid uid, BaseCharger component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !component.HasCell ||
                !_actionBlockerSystem.CanPickup(args.User))
                return;

            Verb verb = new();
            verb.Text = EntityManager.GetComponent<MetaDataComponent>(component.Container.ContainedEntity!.Value).EntityName;
            verb.Category = VerbCategory.Eject;
            verb.Act = () => component.RemoveItem(args.User);
            args.Verbs.Add(verb);
        }

        private void AddInsertVerb(EntityUid uid, BaseCharger component, GetInteractionVerbsEvent args)
        {
            if (args.Using is not {Valid: true} @using ||
                !args.CanAccess ||
                !args.CanInteract ||
                component.HasCell ||
                !component.IsEntityCompatible(@using) ||
                !_actionBlockerSystem.CanDrop(args.User))
                return;

            Verb verb = new();
            verb.Text = EntityManager.GetComponent<MetaDataComponent>(@using).EntityName;
            verb.Category = VerbCategory.Insert;
            verb.Act = () => component.TryInsertItem(@using);
            args.Verbs.Add(verb);
        }
    }
}
