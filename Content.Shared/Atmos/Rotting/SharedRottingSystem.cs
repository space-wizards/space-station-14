using Content.Shared.Changeling;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Atmos.Rotting;

public abstract class SharedRottingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public const int MaxStages = 3;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PerishableComponent, MapInitEvent>(OnPerishableMapInit);
        SubscribeLocalEvent<PerishableComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PerishableComponent, ExaminedEvent>(OnPerishableExamined);

        SubscribeLocalEvent<RottingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RottingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RottingComponent, MobStateChangedEvent>(OnRottingMobStateChanged);
        SubscribeLocalEvent<RottingComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<RottingComponent, ExaminedEvent>(OnExamined);
    }

    private void OnPerishableMapInit(EntityUid uid, PerishableComponent component, MapInitEvent args)
    {
        component.RotNextUpdate = _timing.CurTime + component.PerishUpdateRate;
    }

    private void OnMobStateChanged(EntityUid uid, PerishableComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead && args.OldMobState != MobState.Dead)
            return;

        if (HasComp<RottingComponent>(uid))
            return;

        component.RotAccumulator = TimeSpan.Zero;
        component.RotNextUpdate = _timing.CurTime + component.PerishUpdateRate;
    }

    private void OnPerishableExamined(Entity<PerishableComponent> perishable, ref ExaminedEvent args)
    {
        int stage = PerishStage(perishable, MaxStages);
        if (stage < 1 || stage > MaxStages)
        {
            // We dont push an examined string if it hasen't started "perishing" or it's already rotting
            return;
        }

        var isMob = HasComp<MobStateComponent>(perishable);
        var description = "perishable-" + stage + (!isMob ? "-nonmob" : string.Empty);
        args.PushMarkup(Loc.GetString(description, ("target", Identity.Entity(perishable, EntityManager))));
    }

    private void OnStartup(Entity<RottingComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.NextRotUpdate = _timing.CurTime + ent.Comp.RotUpdateRate;
    }

    private void OnShutdown(EntityUid uid, RottingComponent component, ComponentShutdown args)
    {
        if (TryComp<PerishableComponent>(uid, out var perishable))
        {
            perishable.RotNextUpdate = TimeSpan.Zero;
        }
    }

    private void OnRottingMobStateChanged(EntityUid uid, RottingComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            return;
        RemCompDeferred(uid, component);
    }

    private void OnRejuvenate(EntityUid uid, RottingComponent component, RejuvenateEvent args)
    {
        RemCompDeferred<RottingComponent>(uid);
    }

    private void OnExamined(EntityUid uid, RottingComponent component, ExaminedEvent args)
    {
        var stage = RotStage(uid, component);
        var description = stage switch
        {
            >= 2 => "rotting-extremely-bloated",
            >= 1 => "rotting-bloated",
            _ => "rotting-rotting"
        };

        if (!HasComp<MobStateComponent>(uid))
            description += "-nonmob";

        args.PushMarkup(Loc.GetString(description, ("target", Identity.Entity(uid, EntityManager))));

        if (HasComp<ChangelingComponent>(args.Examiner) && !HasComp<AbsorbedComponent>(uid))
        {
            var changelingDescription = stage switch
            {
                >= 2 => "changeling-examine-extremely-bloated",
                _ => "changeling-examine-rotting"
            };

            args.PushMarkup(Loc.GetString(changelingDescription, ("target", Identity.Entity(uid, EntityManager))));
        }
    }

    /// <summary>
    /// Return an integer from 0 to maxStage representing how close to rotting an entity is. Used to
    /// generate examine messages for items that are starting to rot.
    /// </summary>
    public int PerishStage(Entity<PerishableComponent> perishable, int maxStages)
    {
        if (perishable.Comp.RotAfter.TotalSeconds == 0 || perishable.Comp.RotAccumulator.TotalSeconds == 0)
            return 0;
        return (int)(1 + maxStages * perishable.Comp.RotAccumulator.TotalSeconds / perishable.Comp.RotAfter.TotalSeconds);
    }

    public bool IsRotProgressing(EntityUid uid, PerishableComponent? perishable)
    {
        // things don't perish by default.
        if (!Resolve(uid, ref perishable, false))
            return false;

        // Overrides all the other checks.
        if (perishable.ForceRotProgression)
            return true;

        // only dead things or inanimate objects can rot
        if (TryComp<MobStateComponent>(uid, out var mobState) && !_mobState.IsDead(uid, mobState))
            return false;

        if (_container.TryGetOuterContainer(uid, Transform(uid), out var container) &&
            HasComp<AntiRottingContainerComponent>(container.Owner))
        {
            return false;
        }

        var ev = new IsRottingEvent();
        RaiseLocalEvent(uid, ref ev);

        return !ev.Handled;
    }

    public bool IsRotten(EntityUid uid, RottingComponent? rotting = null)
    {
        return Resolve(uid, ref rotting, false);
    }

    public void ReduceAccumulator(EntityUid uid, TimeSpan time)
    {
        if (!TryComp<PerishableComponent>(uid, out var perishable))
            return;

        if (!TryComp<RottingComponent>(uid, out var rotting))
        {
            perishable.RotAccumulator -= time;
            return;
        }
        var total = (rotting.TotalRotTime + perishable.RotAccumulator) - time;

        if (total < perishable.RotAfter)
        {
            RemCompDeferred(uid, rotting);
            perishable.RotAccumulator = total;
        }

        else
            rotting.TotalRotTime = total - perishable.RotAfter;
    }

    /// <summary>
    /// Return the rot stage, usually from 0 to 2 inclusive.
    /// </summary>
    public int RotStage(EntityUid uid, RottingComponent? comp = null, PerishableComponent? perishable = null)
    {
        if (!Resolve(uid, ref comp, ref perishable))
            return 0;

        return (int) (comp.TotalRotTime.TotalSeconds / perishable.RotAfter.TotalSeconds);
    }
}
