using Content.Server.Power.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Popups;
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

            SubscribeLocalEvent<ServerBatteryBarrelComponent, GetAlternativeVerbsEvent>(AddEjectCellVerb);
            SubscribeLocalEvent<ServerBatteryBarrelComponent, GetInteractionVerbsEvent>(AddInsertCellVerb);

            SubscribeLocalEvent<BoltActionBarrelComponent, GetInteractionVerbsEvent>(AddToggleBoltVerb);

            SubscribeLocalEvent<ServerMagazineBarrelComponent, GetInteractionVerbsEvent>(AddMagazineInteractionVerbs);
            SubscribeLocalEvent<ServerMagazineBarrelComponent, GetAlternativeVerbsEvent>(AddEjectMagazineVerb);
        }

        private void AddSpinVerb(EntityUid uid, RevolverBarrelComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            if (component.Capacity <= 1 || component.ShotsLeft == 0)
                return;

            Verb verb = new();
            verb.Text = Loc.GetString("spin-revolver-verb-get-data-text");
            verb.IconTexture = "/Textures/Interface/VerbIcons/refresh.svg.192dpi.png";
            verb.Act = () =>
            {
                component.Spin();
                component.Owner.PopupMessage(args.User, Loc.GetString("spin-revolver-verb-on-activate"));
            };
            args.Verbs.Add(verb);
        }

        private void AddToggleBoltVerb(EntityUid uid, BoltActionBarrelComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract)
                return;

            Verb verb = new();
            verb.Text = component.BoltOpen
                ? Loc.GetString("close-bolt-verb-get-data-text")
                : Loc.GetString("open-bolt-verb-get-data-text");
            verb.Act = () => component.BoltOpen = !component.BoltOpen;
            args.Verbs.Add(verb);
        }

        // TODO VERBS EJECTABLES Standardize eject/insert verbs into a single system?
        // Really, why isn't this just PowerCellSlotComponent?
        private void AddEjectCellVerb(EntityUid uid, ServerBatteryBarrelComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !component.PowerCellRemovable ||
                component.PowerCell == null ||
                !_actionBlockerSystem.CanPickup(args.User))
                return;

            Verb verb = new();
            verb.Text = component.PowerCell.Owner.Name;
            verb.Category = VerbCategory.Eject;
            verb.Act = () => component.TryEjectCell(args.User);
            args.Verbs.Add(verb);
        }

        private void AddInsertCellVerb(EntityUid uid, ServerBatteryBarrelComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                component.PowerCell != null ||
                !args.Using.HasComponent<BatteryComponent>() ||
                !_actionBlockerSystem.CanDrop(args.User))
                return;

            Verb verb = new();
            verb.Text = args.Using.Name;
            verb.Category = VerbCategory.Insert;
            verb.Act = () => component.TryInsertPowerCell(args.Using);
            args.Verbs.Add(verb);
        }

        private void AddEjectMagazineVerb(EntityUid uid, ServerMagazineBarrelComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !component.HasMagazine ||
                !_actionBlockerSystem.CanPickup(args.User))
                return;

            if (component.MagNeedsOpenBolt && !component.BoltOpen)
                return;

            Verb verb = new();
            verb.Text = component.MagazineContainer.ContainedEntity!.Name;
            verb.Category = VerbCategory.Eject;
            verb.Act = () => component.RemoveMagazine(args.User);
            args.Verbs.Add(verb);
        }

        private void AddMagazineInteractionVerbs(EntityUid uid, ServerMagazineBarrelComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract)
                return;

            // Toggle bolt verb
            Verb toggleBolt = new();
            toggleBolt.Text = component.BoltOpen
                ? Loc.GetString("close-bolt-verb-get-data-text")
                : Loc.GetString("open-bolt-verb-get-data-text");
            toggleBolt.Act = () => component.BoltOpen = !component.BoltOpen;
            args.Verbs.Add(toggleBolt);

            // Are we holding a mag that we can insert?
            if (args.Using == null ||
                !component.CanInsertMagazine(args.User, args.Using) ||
                !_actionBlockerSystem.CanDrop(args.User))
                return;

            // Insert mag verb
            Verb insert = new();
            insert.Text = args.Using.Name;
            insert.Category = VerbCategory.Insert;
            insert.Act = () => component.InsertMagazine(args.User, args.Using);
            args.Verbs.Add(insert);
        }
    }
}
