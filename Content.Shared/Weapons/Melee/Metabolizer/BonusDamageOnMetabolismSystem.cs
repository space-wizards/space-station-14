using System.Linq;
using Content.Shared.Metabolism;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Weapons.Melee.Metabolizer;

public sealed partial class BonusDamageOnMetabolismSystem : EntitySystem
{
    [Dependency] private MetabolizerSystem _metabolizer = default!;

    [SubscribeLocalEvent]
    private void OnGetVerb(Entity<BonusDamageOnMetabolismComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var allMetabolizers = ProtoMan.EnumeratePrototypes<MetabolizerTypePrototype>().ToList().OrderBy(x => Loc.GetString(x.LocalizedName));

        byte index = 0;
        foreach (var metabolizer in allMetabolizers)
        {
            if (ent.Comp.ExcludedMetabolizers.Contains(metabolizer))
                continue;

            var currIndex = index;
            var verb = new Verb
            {
                Priority = currIndex,
                Category = VerbCategory.Metabolizers,
                Disabled = ent.Comp.SelectedMetabolizer == metabolizer,
                Act = () =>
                {
                    ent.Comp.SelectedMetabolizer = metabolizer;
                    Dirty(ent);
                },
                Text = Loc.GetString(metabolizer.LocalizedName),
            };
            args.Verbs.Add(verb);
            index++;
        }
    }

    [SubscribeLocalEvent]
    private void OnSwingTrigger(Entity<BonusDamageOnMetabolismComponent> ent, ref MeleeHitEvent args)
    {
        if (ent.Comp.SelectedMetabolizer is null)
            return;

        foreach (var hitEntity in args.HitEntities)
        {
            if (TryComp<MobStateComponent>(hitEntity, out var mobState) && !ent.Comp.ValidMobStates.Contains(mobState.CurrentState))
                continue;

            if (!_metabolizer.BodyHasMetabolizer(hitEntity, ent.Comp.SelectedMetabolizer.Value))
                continue;

            // Add the bonus damage and quit!
            args.BonusDamage =+ ent.Comp.Damage;
            return;
        }
    }
}
