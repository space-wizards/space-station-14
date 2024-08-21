using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Spillable;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map;

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

        if (!_solutionSystem.TryGetSolution(args.Target, entity.Comp.SolutionName, out var solution))
            return;

        if (Openable.IsClosed(args.Target))
            return;

        if (solution.Comp.Volume == FixedPoint2.Zero)
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

        #region Spill
    // These methods are in Shared to make it easier to interact with PuddleSystem in Shared code.
    // Note that they always fail when run on the client, not creating a puddle and returning false.
    // Adding proper prediction to this system would require spawning temporary puddle entities on the
    // client and replacing or merging them with the ones spawned by the server when the client goes to
    // replicate those, and I am not enough of a wizard to attempt implementing that.

    /// <summary>
    ///     First splashes reagent on reactive entities near the spilling entity, then spills the rest regularly to a
    ///     puddle. This is intended for 'destructive' spills, like when entities are destroyed or thrown.
    /// </summary>
    /// <remarks>
    /// On the client, this will always set <paramref name="puddleUid"/> to <see cref="EntityUid.Invalid"> and return false.
    /// </remarks>
    public abstract bool TrySplashSpillAt(EntityUid uid,
        EntityCoordinates coordinates,
        Entity<SolutionComponent>  solution,
        out EntityUid puddleUid,
        bool sound = true,
        EntityUid? user = null);

    /// <summary>
    ///     Spills solution at the specified coordinates.
    /// Will add to an existing puddle if present or create a new one if not.
    /// </summary>
    /// <remarks>
    /// On the client, this will always set <paramref name="puddleUid"/> to <see cref="EntityUid.Invalid"> and return false.
    /// </remarks>
    public abstract bool TrySpillAt(EntityCoordinates coordinates,
        Entity<SolutionComponent>  solution,
        out EntityUid puddleUid,
        bool sound = true);

    /// <summary>
    /// <see cref="TrySpillAt(EntityCoordinates, Solution, out EntityUid, bool)"/>
    /// </summary>
    /// <remarks>
    /// On the client, this will always set <paramref name="puddleUid"/> to <see cref="EntityUid.Invalid"> and return false.
    /// </remarks>
    public abstract bool TrySpillAt(EntityUid uid,
        Entity<SolutionComponent>  solution,
        out EntityUid puddleUid,
        bool sound = true,
        TransformComponent? transformComponent = null);

    /// <summary>
    /// <see cref="TrySpillAt(EntityCoordinates, Solution, out EntityUid, bool)"/>
    /// </summary>
    /// <remarks>
    /// On the client, this will always set <paramref name="puddleUid"/> to <see cref="EntityUid.Invalid"> and return false.
    /// </remarks>
    public abstract bool TrySpillAt(TileRef tileRef,
        Entity<SolutionComponent>  solution,
        out EntityUid puddleUid,
        bool sound = true,
        bool tileReact = true);

    #endregion Spill
}
