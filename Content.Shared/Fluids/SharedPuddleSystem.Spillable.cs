using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Spillable;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    [Dependency] protected readonly OpenableSystem Openable = default!;

    protected virtual void InitializeSpillable()
    {
        SubscribeLocalEvent<SpillableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SpillableComponent, GetVerbsEvent<Verb>>(AddSpillVerb);
    }

    private void OnExamined(Entity<SpillableComponent> entity, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(SpillableComponent)))
        {
            args.PushMarkup(Loc.GetString("spill-examine-is-spillable"));

            if (HasComp<MeleeWeaponComponent>(entity))
                args.PushMarkup(Loc.GetString("spill-examine-spillable-weapon"));
        }
    }

    private void AddSpillVerb(Entity<SpillableComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_solutionContainerSystem.TryGetSolution(args.Target, entity.Comp.SolutionName, out var soln, out var solution))
            return;

        if (Openable.IsClosed(args.Target))
            return;

        if (solution.Volume == FixedPoint2.Zero)
            return;

        if (HasComp<PreventSpillerComponent>(args.User))
            return;


        Verb verb = new()
        {
            Text = Loc.GetString("spill-target-verb-get-data-text")
        };

        // TODO VERB ICONS spill icon? pouring out a glass/beaker?
        if (entity.Comp.SpillDelay == null)
        {
            var target = args.Target;
            verb.Act = () =>
            {
                var puddleSolution = _solutionContainerSystem.SplitSolution(soln.Value, solution.Volume);
                TrySpillAt(Transform(target).Coordinates, puddleSolution, out _);

                if (TryComp<InjectorComponent>(entity, out var injectorComp))
                {
                    injectorComp.ToggleState = InjectorToggleMode.Draw;
                    Dirty(entity, injectorComp);
                }
            };
        }
        else
        {
            var user = args.User;
            verb.Act = () =>
            {
                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, entity.Comp.SpillDelay ?? 0, new SpillDoAfterEvent(), entity.Owner, target: entity.Owner)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                });
            };
        }
        verb.Impact = LogImpact.Medium; // dangerous reagent reaction are logged separately.
        verb.DoContactInteraction = true;
        args.Verbs.Add(verb);
    }
}
