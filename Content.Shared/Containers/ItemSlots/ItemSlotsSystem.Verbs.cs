using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Containers.ItemSlots;

public sealed partial class ItemSlotsSystem
{
    [SubscribeLocalEvent]
    private void AddAlternativeVerbs(Entity<ItemSlotsComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        if (args.Using != null && _actionBlockerSystem.CanDrop(args.User))
        {
            var usingEntity = args.Using.Value;
            var canInsertAny = false;
            foreach (var slot in ent.Comp.Slots.Values)
            {
                if (slot.InsertOnInteract || !CanInsert(ent, usingEntity, user, slot))
                    continue;

                var verbSubject = slot.Name != string.Empty
                    ? Loc.GetString(slot.Name)
                    : Name(usingEntity);

                AlternativeVerb verb = new()
                {
                    IconEntity = GetNetEntity(usingEntity),
                    Act = () => Insert(ent, slot, usingEntity, user, excludeUserAudio: true)
                };

                if (slot.InsertVerbText != null)
                {
                    verb.Text = Loc.GetString(slot.InsertVerbText);
                    verb.Icon = new SpriteSpecifier.Texture(
                        new("/Textures/Interface/VerbIcons/insert.svg.192dpi.png"));
                }
                else if (slot.EjectOnInteract)
                {
                    verb.Text = Loc.GetString("place-item-verb-text", ("subject", verbSubject));
                    verb.Icon = new SpriteSpecifier.Texture(
                        new("/Textures/Interface/VerbIcons/drop.svg.192dpi.png"));
                }
                else
                {
                    verb.Category = VerbCategory.Insert;
                    verb.Text = verbSubject;
                }

                verb.Priority = slot.Priority;
                args.Verbs.Add(verb);
                canInsertAny = true;
            }

            if (canInsertAny)
                return;
        }

        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (slot.EjectOnInteract || slot.DisableEject)
                continue;

            if (!CanEject(ent, user, slot))
                continue;

            if (!_actionBlockerSystem.CanPickup(user, slot.Item!.Value))
                continue;

            var verbSubject = slot.Name != string.Empty
                ? Loc.GetString(slot.Name)
                : Comp<MetaDataComponent>(slot.Item.Value).EntityName ?? string.Empty;

            AlternativeVerb verb = new()
            {
                IconEntity = GetNetEntity(slot.Item),
                Act = () => TryEjectToHands(ent, slot, user, excludeUserAudio: true)
            };

            if (slot.EjectVerbText == null)
            {
                verb.Text = verbSubject;
                verb.Category = VerbCategory.Eject;
            }
            else
            {
                verb.Text = Loc.GetString(slot.EjectVerbText);
            }

            verb.Priority = slot.Priority;
            args.Verbs.Add(verb);
        }
    }

    [SubscribeLocalEvent]
    private void AddInteractionVerbs(Entity<ItemSlotsComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (!slot.EjectOnInteract || !CanEject(ent, user, slot))
                continue;

            if (!_actionBlockerSystem.CanPickup(user, slot.Item!.Value))
                continue;

            var verbSubject = slot.Name != string.Empty
                ? Loc.GetString(slot.Name)
                : Name(slot.Item!.Value);

            InteractionVerb takeVerb = new()
            {
                IconEntity = GetNetEntity(slot.Item),
                Act = () => TryEjectToHands(ent, slot, user, excludeUserAudio: true)
            };

            if (slot.EjectVerbText == null)
                takeVerb.Text = Loc.GetString("take-item-verb-text", ("subject", verbSubject));
            else
                takeVerb.Text = Loc.GetString(slot.EjectVerbText);

            takeVerb.Priority = slot.Priority;
            args.Verbs.Add(takeVerb);
        }

        if (args.Using == null || !_actionBlockerSystem.CanDrop(user))
            return;

        var usingEntity = args.Using.Value;
        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (!slot.InsertOnInteract || !CanInsert(ent, usingEntity, user, slot))
                continue;

            var verbSubject = slot.Name != string.Empty
                ? Loc.GetString(slot.Name)
                : Name(usingEntity);

            InteractionVerb insertVerb = new()
            {
                IconEntity = GetNetEntity(usingEntity),
                Act = () => Insert(ent, slot, usingEntity, user, excludeUserAudio: true)
            };

            if (slot.InsertVerbText != null)
            {
                insertVerb.Text = Loc.GetString(slot.InsertVerbText);
                insertVerb.Icon =
                    new SpriteSpecifier.Texture(
                        new ResPath("/Textures/Interface/VerbIcons/insert.svg.192dpi.png"));
            }
            else if (slot.EjectOnInteract)
            {
                insertVerb.Text = Loc.GetString("place-item-verb-text", ("subject", verbSubject));
                insertVerb.Icon =
                    new SpriteSpecifier.Texture(
                        new ResPath("/Textures/Interface/VerbIcons/drop.svg.192dpi.png"));
            }
            else
            {
                insertVerb.Category = VerbCategory.Insert;
                insertVerb.Text = verbSubject;
            }

            insertVerb.Priority = slot.Priority;
            args.Verbs.Add(insertVerb);
        }
    }
}
