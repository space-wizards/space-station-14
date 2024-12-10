using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Spillable;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{

    protected EntityQuery<MapGridComponent> MapGridQuery;
    protected EntityQuery<PuddleComponent> PuddleQuery;
    protected EntityQuery<SolutionComponent> SolutionQuery;
    protected EntityQuery<SolutionHolderComponent> SolutionHolderQuery;

    protected virtual void InitializeSpillable()
    {
        SubscribeLocalEvent<SpillableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SpillableComponent, GetVerbsEvent<Verb>>(AddSpillVerb);
        MapGridQuery = EntityManager.GetEntityQuery<MapGridComponent>();
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

        if (!SolutionSystem.TryGetSolution(args.Target, entity.Comp.SolutionName, out var solution))
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
                TrySpillAt(Transform(target).Coordinates, solution, out _);

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
                DoAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
                    user,
                    entity.Comp.SpillDelay ?? 0,
                    new SpillDoAfterEvent(),
                    entity.Owner,
                    target: entity.Owner)
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

    public bool TryGetPuddleEntity(EntityCoordinates targetLocation,
        out Entity<PuddleComponent, SolutionHolderComponent?> puddle,
        out Entity<SolutionComponent> puddleSolution)
    {
        MapGridComponent? mapGrid = null;
        if (TryGetTileRef(targetLocation, ref mapGrid, out var tileRef)
            && CanSpillOnTile(tileRef, ref mapGrid))
            return TryGetPuddleEntity(tileRef, out puddle, out puddleSolution, mapGrid);
        puddle = default;
        puddleSolution = default;
        return false;
    }

    public bool TryGetPuddleEntity(TileRef targetTile,
        out Entity<PuddleComponent, SolutionHolderComponent?> puddle,
        out Entity<SolutionComponent> puddleSolution,
        MapGridComponent? mapGrid = null)
    {
        if (CanSpillOnTile(targetTile, ref mapGrid))
            return TryGetPuddle_Implementation(targetTile, mapGrid, out puddle, out puddleSolution);
        puddle = default;
        puddleSolution = default;
        return false;
    }

    public bool TryEnsurePuddle(EntityCoordinates targetLocation,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        out Entity<SolutionComponent> puddleSolution)
    {
        MapGridComponent? mapGrid = null;
       if (TryGetTileRef(targetLocation, ref mapGrid, out var tileRef)
           && TryEnsurePuddle(tileRef, out puddle, out puddleSolution, mapGrid))
           return true;
       puddle = default;
       puddleSolution = default;
       return false;
    }

    public bool TryEnsurePuddle(TileRef targetTile,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        out Entity<SolutionComponent> puddleSolution,
        MapGridComponent? mapGrid = null)
    {
        if (!CanSpillOnTile(targetTile, ref mapGrid))
        {
            puddle = default;
            puddleSolution = default;
            return false;
        }

        if (TryGetPuddle_Implementation(targetTile,
                mapGrid,
                out var existingPuddle,
                out var existingSolution))
        {
            if (existingPuddle.Comp2 == null)
            {
                if (!SolutionSystem.TryEnsureSolution((existingPuddle.Owner, existingPuddle.Comp2, null),
                        PuddleSolutionId,
                        out existingSolution))
                {
                    puddle = default;
                    puddleSolution = default;
                    return false;
                }
            }
            puddle = (existingPuddle.Owner, existingPuddle.Comp1, existingPuddle.Comp2!);
            puddleSolution = existingSolution;
            return true;
        }

        if (CreatePuddle(targetTile, mapGrid, out var newPuddle, out puddleSolution))
        {
            puddle = (newPuddle, newPuddle, newPuddle);
            return true;
        }
        puddle = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetPuddle_Implementation(TileRef targetTile,
        MapGridComponent mapGrid,
        out Entity<PuddleComponent, SolutionHolderComponent?> puddle,
        out Entity<SolutionComponent> puddleSolution)
    {
        var anchored = MapSystem.GetAnchoredEntitiesEnumerator(targetTile.GridUid, mapGrid, targetTile.GridIndices);
        while (anchored.MoveNext(out var foundEnt))
        {
            if (!PuddleQuery.TryComp(foundEnt, out var puddleComp))
                continue;
            SolutionHolderQuery.TryComp(foundEnt, out var solHolderComp);
            puddle = (foundEnt.Value, puddleComp, solHolderComp);
            puddleSolution = default;
            return true;
        }
        puddle = default;
        puddleSolution = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual bool CreatePuddle(TileRef targetTile,
        MapGridComponent mapGrid,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        out Entity<SolutionComponent> puddleSolution)
    {
        var coords = MapSystem.GridTileToLocal(targetTile.GridUid, mapGrid, targetTile.GridIndices);
        var puddleEnt = EntityManager.SpawnEntity("Puddle", coords);
        EnsureComp<PuddleComponent>(puddleEnt, out var puddleComp);
        EnsureComp<SolutionHolderComponent>(puddleEnt, out var solutionHolder);

        if (SolutionSystem.TryEnsureSolution((puddleEnt, solutionHolder, null),
                PuddleSolutionId,
                out puddleSolution))
        {
            puddle = (puddleEnt, puddleComp, solutionHolder);
            return false;
        }
        puddle = (puddleEnt, puddleComp, solutionHolder);
        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanSpillOnTile(TileRef targetTile, [NotNullWhen(true)] ref MapGridComponent? mapGrid)
    {
        return MapGridQuery.Resolve(targetTile.GridUid, ref mapGrid)
            || targetTile.Tile.IsEmpty
            || targetTile.IsSpace(TileDefManager);
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
    public bool TrySplashSpillAt(
        EntityCoordinates targetCoords,
        Entity<SolutionComponent> solution,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        float splashedPercentage = 1.0f,
        float range = 1.0f,
        bool sound = true,
        EntityUid? user = null)
    {
        var targets = new List<EntityUid>();
        var reactive = new HashSet<Entity<ReactiveComponent>>();
        EntityLookup.GetEntitiesInRange(targetCoords, range, reactive);
        splashedPercentage = Math.Clamp(splashedPercentage, 0, 1.0f);
        if (splashedPercentage > 0)
        {
            foreach (var targetEnt in reactive)
            {
                var splitPercentage = splashedPercentage*RobustRandom.NextFloat(MinSplashPercentage, MaxSplashPercentage);
                if (user != null)
                {
                    AdminLogger.Add(LogType.Landed,
                        $"{ToPrettyString(user.Value):user} threw {ToPrettyString(solution.Comp.Parent):entity}" +
                        $" which splashed a solution {SharedSolutionSystem.ToPrettyString(solution):solution} onto " +
                        $"{ToPrettyString(targetEnt):target}");
                }
                targets.Add(targetEnt);
                if (TryGetTileRef(targetCoords, out var targetTile))
                    SolutionSystem.DoTileReactions(targetTile, solution, splitPercentage);
                PopupSystem.PopupEntity(
                    Loc.GetString("spill-land-spilled-on-other",
                        ("spillable", solution.Comp.Parent),
                        ("target", Identity.Entity(targetEnt.Owner, EntityManager))),
                    targetEnt,
                    PopupType.SmallCaution);
            }
        }
        ColorFlashSystem.RaiseEffect(SolutionSystem.GetSolutionColor(solution),
            targets,
            Filter.Pvs(solution.Comp.Parent, entityManager: EntityManager));
        return TrySpillAt(targetCoords, solution, out puddle, sound);
    }

    public bool TrySplashSpillAt(
        EntityUid source,
        EntityCoordinates targetCoords,
        SolutionContents solutionContents,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        float splashedPercentage = 1.0f,
        float range = 1.0f,
        bool sound = true,
        EntityUid? user = null)
    {
        var targets = new List<EntityUid>();
        var reactive = new HashSet<Entity<ReactiveComponent>>();
        EntityLookup.GetEntitiesInRange(targetCoords, range, reactive);
        splashedPercentage = Math.Clamp(splashedPercentage, 0, 1.0f);
        if (splashedPercentage > 0)
        {
            foreach (var targetEnt in reactive)
            {
                var splitPercentage = splashedPercentage*RobustRandom.NextFloat(MinSplashPercentage, MaxSplashPercentage);
                if (user != null)
                {
                    AdminLogger.Add(LogType.Landed,
                        $"{ToPrettyString(user.Value):user} threw {ToPrettyString(source):entity}" +
                        $" which splashed a solution {SharedSolutionSystem.ToPrettyString(null,solutionContents):solution} onto " +
                        $"{ToPrettyString(targetEnt):target}");
                }
                targets.Add(targetEnt);
                if (TryGetTileRef(targetCoords, out var targetTile))
                    solutionContents = ReactiveSystem.DoTileReactions(targetTile, solutionContents, splashedPercentage);
                PopupSystem.PopupEntity(
                    Loc.GetString("spill-land-spilled-on-other",
                        ("spillable", source),
                        ("target", Identity.Entity(targetEnt.Owner, EntityManager))),
                    targetEnt,
                    PopupType.SmallCaution);
            }
        }
        ColorFlashSystem.RaiseEffect(SolutionSystem.GetSolutionColor(solutionContents),
            targets,
            Filter.Pvs(source, entityManager: EntityManager));
        return TrySpillAt(targetCoords, solutionContents, out puddle, sound);
    }

    public bool TrySpillAt(EntityCoordinates targetLocation,
        Entity<SolutionComponent> sourceSolution,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        bool sound = true,
        bool tileReact = true)
    {
        return TrySpillAt(targetLocation, sourceSolution, sourceSolution.Comp.Volume, out puddle, sound, tileReact);
    }

    public bool TrySpillAt(EntityCoordinates targetLocation,
        Entity<SolutionComponent> sourceSolution,
        FixedPoint2 quantity,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        bool sound = true,
        bool tileReact = true)
    {
        TryGetTileRef(targetLocation, out var tileRef);
        return TrySpillAt(tileRef, sourceSolution, sourceSolution.Comp.Volume, out puddle, sound, tileReact);
    }

    public bool TrySpillAt(EntityCoordinates targetLocation,
        SolutionContents solutionContents,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        bool sound = true,
        bool tileReact = true)
    {
        MapGridComponent? mapgrid = null;
        if (!TryGetTileRef(targetLocation, ref mapgrid, out var targetTile))
        {
            puddle = default;
            return false;
        }
        return TrySpillAt(targetTile, solutionContents, out puddle, sound, tileReact);
    }

    public bool TrySpillAt(EntityUid uid,
        Entity<SolutionComponent> solution,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        bool sound = true,
        bool tileReact = true)
    {
        return TrySpillAt(new EntityCoordinates(uid, 0, 0), solution, out puddle, sound, tileReact);
    }

    public bool TrySpillAt(EntityUid uid,
        Entity<SolutionComponent> solution,
        FixedPoint2 quantity,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        bool sound = true,
        bool tileReact = true)
    {
        return TrySpillAt(new EntityCoordinates(uid, 0, 0), solution, quantity, out puddle, sound, tileReact);
    }

    public bool TrySpillAt(EntityUid uid,
        SolutionContents solutionContents,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        bool sound = true,
        bool tileReact = true)
    {
        return TrySpillAt(new EntityCoordinates(uid, 0, 0), solutionContents, out puddle, sound, tileReact);
    }

    public bool TrySpillAt(TileRef targetTile,
        Entity<SolutionComponent> sourceSolution,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        bool sound = true,
        bool tileReact = true)
    {
        return TrySpillAt(targetTile, sourceSolution, sourceSolution.Comp.Volume, out puddle, sound, tileReact);
    }

    public bool TrySpillAt(TileRef targetTile,
        Entity<SolutionComponent> sourceSolution,
        FixedPoint2 quantity,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        bool sound = true,
        bool tileReact = true)
    {
        if (tileReact)
        {
            quantity -= SolutionSystem.DoTileReactions(targetTile,
                sourceSolution,
                quantity.Float() / sourceSolution.Comp.Volume.Float());
        }
        if (TryEnsurePuddle(targetTile,
                out puddle,
                out var puddleSolution))
        {
            //We don't care about overflow because puddle solutions don't have a max volume and control
            //their own overflowing/spreading logic
            if (quantity > 0)
                SolutionSystem.TransferSolution(sourceSolution, puddleSolution, quantity, out _);
            if (sound)
                AudioSystem.PlayPvs(puddle.Comp1.SpillSound, puddle);
            return true;
        }
        if (quantity > 0)//Predict removing the reagents even if we aren't able to create/find a puddle.
            SolutionSystem.RemoveReagents(sourceSolution, quantity);
        return false;
    }

    public bool TrySpillAt(TileRef targetTile,
        SolutionContents solutionContents,
        out Entity<PuddleComponent, SolutionHolderComponent> puddle,
        bool sound = true,
        bool tileReact = true)
    {
        if (tileReact)
            solutionContents = ReactiveSystem.DoTileReactions(targetTile, solutionContents);
        if (TryEnsurePuddle(targetTile,
                out puddle,
                out var puddleSolution))
        {
            //We don't care about overflow because puddle solutions don't have a max volume and control
            //their own overflowing/spreading logic
            if (solutionContents.Volume <= 0)
                return true;

            SolutionSystem.AddReagents(puddleSolution, solutionContents.Temperature, reagents:solutionContents);
            if (sound)
                AudioSystem.PlayPvs(puddle.Comp1.SpillSound, puddle);
            return true;
        }
        return false;
    }

    #endregion Spill

    private bool TryGetTileRef(EntityCoordinates coordinates, out TileRef targetTile)
    {
        var gridEnt = TransformSystem.GetGrid(coordinates);
        if (gridEnt == null || !MapGridQuery.TryComp(gridEnt, out var mapGrid))
        {
            targetTile = TileRef.Zero;
            return false;
        }
        targetTile = MapSystem.GetTileRef(gridEnt.Value, mapGrid, coordinates);
        return true;
    }

    private bool TryGetTileRef(EntityCoordinates coordinates, ref MapGridComponent? mapGrid, out TileRef targetTile)
    {
        var gridEnt = TransformSystem.GetGrid(coordinates);
        if (gridEnt == null || !MapGridQuery.Resolve(gridEnt.Value, ref mapGrid))
        {
            targetTile = TileRef.Zero;
            return false;
        }
        targetTile = MapSystem.GetTileRef(gridEnt.Value, mapGrid, coordinates);
        return true;
    }
}
