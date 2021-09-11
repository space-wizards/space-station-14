using Content.Server.Power.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Notification.Managers;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Weapon.Ranged.Barrels
{
    public sealed class BarrelSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem  _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RevolverBarrelComponent, GetAlternativeVerbsEvent>(AddSpinVerb);
            SubscribeLocalEvent<ServerBatteryBarrelComponent, GetAlternativeVerbsEvent>(AddEjectVerb);
            SubscribeLocalEvent<ServerBatteryBarrelComponent, GetInteractionVerbsEvent>(AddInsertVerb);
            SubscribeLocalEvent<BoltActionBarrelComponent, GetInteractionVerbsEvent>(AddToggleBoltVerb);
        }

        private void AddToggleBoltVerb(EntityUid uid, BoltActionBarrelComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract)
                return;

            Verb verb = new("bolt:toggle");
            verb.Text = component.BoltOpen
                ? Loc.GetString("close-bolt-verb-get-data-text")
                : Loc.GetString("open-bolt-verb-get-data-text");
            verb.Act = () => component.BoltOpen = !component.BoltOpen;
            args.Verbs.Add(verb);
        }

        // TODO VERBS EJECTABLES Standardize eject/insert verbs into a single system?
        // Really, why isn't this just PowerCellSlotComponent?
        private void AddEjectVerb(EntityUid uid, ServerBatteryBarrelComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !component.PowerCellRemovable ||
                component.PowerCell == null ||
                !_actionBlockerSystem.CanPickup(args.User))
                return;

            Verb verb = new("guncell:eject");
            verb.Text = component.PowerCell.Owner.Name;
            verb.Category = VerbCategory.Eject;
            verb.Act = () => component.TryEjectCell(args.User);
            args.Verbs.Add(verb);
        }

        private void AddInsertVerb(EntityUid uid, ServerBatteryBarrelComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                component.PowerCell != null ||
                !args.Using.HasComponent<BatteryComponent>() ||
                !_actionBlockerSystem.CanDrop(args.User))
                return;

            Verb verb = new("guncell:Insert");
            verb.Text = args.Using.Name;
            verb.Category = VerbCategory.Insert;
            verb.Act = () => component.TryInsertPowerCell(args.Using);
            args.Verbs.Add(verb);
        }

        private void AddSpinVerb(EntityUid uid, RevolverBarrelComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            if (component.Capacity <= 1 || component.ShotsLeft == 0)
                return;

            Verb verb = new Verb("revolverspin");
            verb.Text = Loc.GetString("spin-revolver-verb-get-data-text");
            verb.IconTexture = "/Textures/Interface/VerbIcons/refresh.svg.192dpi.png";
            verb.Act = () =>
            {
                component.Spin();
                component.Owner.PopupMessage(args.User, Loc.GetString("spin-revolver-verb-on-activate"));
            };
            args.Verbs.Add(verb);
        }
    }
}
