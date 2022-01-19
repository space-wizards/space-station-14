using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Weapon.Ranged.Barrels
{
    public sealed class BarrelSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BatteryBarrelComponent, PowerCellChangedEvent>(OnCellSlotUpdated);

            SubscribeLocalEvent<BoltActionBarrelComponent, GetInteractionVerbsEvent>(AddToggleBoltVerb);

            SubscribeLocalEvent<ServerMagazineBarrelComponent, GetInteractionVerbsEvent>(AddMagazineInteractionVerbs);
            SubscribeLocalEvent<ServerMagazineBarrelComponent, GetAlternativeVerbsEvent>(AddEjectMagazineVerb);
        }

        private void OnCellSlotUpdated(EntityUid uid, BatteryBarrelComponent component, PowerCellChangedEvent args)
        {
            component.UpdateAppearance();
        }

        private void AddToggleBoltVerb(EntityUid uid, BoltActionBarrelComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract)
                return;

            Verb verb = new()
            {
                Text = component.BoltOpen
                    ? Loc.GetString("close-bolt-verb-get-data-text")
                    : Loc.GetString("open-bolt-verb-get-data-text"),
                Act = () => component.BoltOpen = !component.BoltOpen
            };
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

            Verb verb = new()
            {
                Text = MetaData(component.MagazineContainer.ContainedEntity!.Value).EntityName,
                Category = VerbCategory.Eject,
                Act = () => component.RemoveMagazine(args.User)
            };
            args.Verbs.Add(verb);
        }

        private void AddMagazineInteractionVerbs(EntityUid uid, ServerMagazineBarrelComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract)
                return;

            // Toggle bolt verb
            Verb toggleBolt = new()
            {
                Text = component.BoltOpen
                    ? Loc.GetString("close-bolt-verb-get-data-text")
                    : Loc.GetString("open-bolt-verb-get-data-text"),
                Act = () => component.BoltOpen = !component.BoltOpen
            };
            args.Verbs.Add(toggleBolt);

            // Are we holding a mag that we can insert?
            if (args.Using is not {Valid: true} @using ||
                !component.CanInsertMagazine(args.User, @using) ||
                !_actionBlockerSystem.CanDrop(args.User))
                return;

            // Insert mag verb
            Verb insert = new()
            {
                Text = MetaData(@using).EntityName,
                Category = VerbCategory.Insert,
                Act = () => component.InsertMagazine(args.User, @using)
            };
            args.Verbs.Add(insert);
        }
    }
}
