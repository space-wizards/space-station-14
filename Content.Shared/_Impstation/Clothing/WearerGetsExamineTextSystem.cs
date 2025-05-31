using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory.Events;
using Content.Shared.Contraband;
using Content.Shared._Impstation.Examine;
using System.Text;

namespace Content.Shared._Impstation.Clothing;

/// <summary>
/// Adds examine text to the entity that wears item, for making things obvious.
/// </summary>
public sealed class WearerGetsExamineTextSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WearerGetsExamineTextComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<WearerGetsExamineTextComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<WearerGetsExamineTextComponent, ExaminedEvent>(OnExamine);
    }

    private void OnEquipped(Entity<WearerGetsExamineTextComponent> entity, ref GotEquippedEvent args)
    {
        if (!TryComp(entity, out ClothingComponent? clothing))
            return;
        var isCorrectSlot = (clothing.Slots & args.SlotFlags) != Inventory.SlotFlags.NONE;
        if (!entity.Comp.PocketEvident) //if it can't be evident in our pockets
        {
            // Make sure the clothing item was equipped to the right slot, and not just held in a hand.
            if (!isCorrectSlot)
                return;
        }

        entity.Comp.Wearer = args.Equipee;
        Dirty(entity);

        //GIVE THEM INSPECT TEXT
        var obviousExamine = EnsureComp<ExtraExamineTextComponent>(args.Equipee);
        obviousExamine.Lines.TryAdd(entity.Owner,  //using try so that we don't cause an error if we move something from slot to slot
            ConstructExamineText(entity, !isCorrectSlot, args.Equipee));
    }


    private string ConstructExamineText(Entity<WearerGetsExamineTextComponent> entity, bool prefixFallback, EntityUid affecting)
    {
        //parameters (these are the same between both constructions)
        var user = Identity.Entity(affecting, EntityManager);
        var nomen = Identity.Name(affecting, EntityManager);
        var thing = Loc.GetString(entity.Comp.Category);
        var type = Loc.GetString(entity.Comp.Specifier);
        var stringSpec = entity.Comp.Specifier.ToString();
        var shortType = stringSpec.Substring(stringSpec.LastIndexOf('-'));  // necessary for working with colored text...

        var prefix = Loc.GetString(prefixFallback ? "obvious-prefix-default" : entity.Comp.PrefixExamineOnWearer, // uses a different prefix if worn / displayed
                ("user", user),
                ("name", nomen),
                ("thing", thing),
                ("type", type));
        var suffix = Loc.GetString(entity.Comp.ExamineOnWearer,
                ("user", user),
                ("name", nomen),
                ("thing", thing),
                ("type", type),
                ("short-type", shortType));
        return prefix + " " + suffix;
    }

    private void OnUnequipped(Entity<WearerGetsExamineTextComponent> entity, ref GotUnequippedEvent args)
    {
        if (entity.Comp.Wearer is not { } wearer)
            return;

        if (TryComp(wearer, out ExtraExamineTextComponent? obviousExamine))
        {
            obviousExamine.Lines.Remove(entity.Owner);
        }

        entity.Comp.Wearer = null;
        Dirty(entity);
    }

    private void OnExamine(Entity<WearerGetsExamineTextComponent> entity, ref ExaminedEvent args)
    {
        var currentlyWorn = entity.Comp.Wearer != null;
        var outString = new StringBuilder(Loc.GetString(currentlyWorn ? "obvious-on-item-currently" : "obvious-on-item",
            ("used", Loc.GetString(entity.Comp.PocketEvident ? "obvious-reveal-pockets" : "obvious-reveal-default")),
            ("thing", entity.Comp.Category),
            ("me", Identity.Entity(entity, EntityManager))));

        if (entity.Comp.WarnExamine)
        {
            if (!currentlyWorn && TryComp(entity, out ContrabandComponent? contra)) // if the item's contra and we're not wearing it yet
            {
                var contraLocId = "obvious-on-item-contra-" + contra.Severity; // apply additional text if the item is contraband to note that displaying it might be really bad
                if (Loc.HasString(contraLocId)) // saves us the trouble of making a switch block for this
                    outString.Append(" " + Loc.GetString(contraLocId));
            }
            var affecting = currentlyWorn ? entity.Comp.Wearer.GetValueOrDefault() : args.Examiner;
            var testOut = ConstructExamineText(entity, false, affecting);

            outString.Append("\n" + Loc.GetString("obvious-on-item-for-others",
                ("will", currentlyWorn ? "can" : "will"), // i love hardcoding strings it's my favorite thing ever
                ("output", testOut)));
        }

        args.PushMarkup(outString.ToString());
    }
}
