using Content.Server.DeadSpace.Abilities.Cocoon.Components;
using Robust.Shared.Containers;
using Content.Server.Body.Components;
using Robust.Shared.Timing;
using Content.Shared.Destructible;
using Content.Server.Body.Systems;
using Content.Server.Atmos.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Speech.Muting;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Body.Events;

namespace Content.Server.DeadSpace.Abilities.Cocoon;

public sealed class CocoonSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    private ISawmill _sawmill = default!;

    const float Factor = 1f;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("CocoonSystem");

        SubscribeLocalEvent<CocoonComponent, BeingGibbedEvent>(OnGibbed);
        SubscribeLocalEvent<CocoonComponent, InsertIntoCocoonEvent>(OnInsert);
        SubscribeLocalEvent<CocoonComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CocoonComponent, ComponentShutdown>(OnShutDown);
        SubscribeLocalEvent<CocoonComponent, DestructionEventArgs>(OnDestruction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var cocoons = EntityQueryEnumerator<CocoonComponent>();
        while (cocoons.MoveNext(out var uid, out var component))
        {
            if (_gameTiming.CurTime > component.NextTick)
            {
                UpdateCocoon(uid, component);
            }
        }
    }

    public bool IsEntityInCocoon(EntityUid uid, EntityUid target, CocoonComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        return _container.IsEntityInContainer(target);
    }

    private void OnMapInit(EntityUid uid, CocoonComponent component, MapInitEvent args)
    {
        component.NextTick = _gameTiming.CurTime + TimeSpan.FromSeconds(1);
        component.Cocoon = _container.EnsureContainer<Container>(uid, "cocoon");
    }

    private void OnInsert(EntityUid uid, CocoonComponent component, InsertIntoCocoonEvent args)
    {
        var target = args.Target;

        Insert(uid, target, component);
    }

    private void OnGibbed(EntityUid uid, CocoonComponent component, BeingGibbedEvent args)
    {
        EmptyCocoon(uid);
    }

    private void OnShutDown(EntityUid uid, CocoonComponent component, ComponentShutdown args)
    {
        EmptyCocoon(uid);
    }

    private void OnDestruction(EntityUid uid, CocoonComponent component, DestructionEventArgs args)
    {
        EmptyCocoon(uid);
    }

    public bool TryInsertCocoon(EntityUid uid, EntityUid target, CocoonComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (_container.IsEntityOrParentInContainer(target))
            return false;

        var insertIntoCocoon = new InsertIntoCocoonEvent(target);
        RaiseLocalEvent(uid, ref insertIntoCocoon);

        return true;
    }

    public EntityUid? GetPrisoner(EntityUid uid, CocoonComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        return component.Prisoner;
    }

    public void EmptyCocoon(EntityUid uid, CocoonComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var target = component.Prisoner;

        if (target == null)
        {
            _sawmill.Warning("Prisoner target is null in EmptyCocoon.");
            return;
        }

        if (IsEntityInCocoon(uid, target.Value, component))
            _container.EmptyContainer(component.Cocoon);

        if (!component.IsHermetically)
            return;

        if (HasComp<MutedComponent>(target) && !component.Mute)
            RemComp<MutedComponent>(target.Value);

        if (HasComp<TemporaryBlindnessComponent>(target) && !component.Blindable)
            RemComp<TemporaryBlindnessComponent>(target.Value);

        if (HasComp<PacifiedComponent>(target) && !component.Pacified)
            RemComp<PacifiedComponent>(target.Value);

        if (HasComp<PressureImmunityComponent>(target) && !component.Pressure)
        {
            _sawmill.Info("Adding BarotraumaComponent back to target.");
            RemComp<PressureImmunityComponent>(target.Value);
        }
        else
        {
            _sawmill.Warning("BarotraumaComponent is either already present or null.");
        }
    }

    public void UpdateCocoon(EntityUid uid, CocoonComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.Prisoner == null)
            return;

        if (!component.IsHermetically)
            return;

        if (TryComp<RespiratorComponent>(component.Prisoner, out var resp))
        {
            _respirator.UpdateSaturation(component.Prisoner.Value, Factor, resp);
        }

        component.NextTick = _gameTiming.CurTime + TimeSpan.FromSeconds(1);
    }

    private void Insert(EntityUid uid, EntityUid target, CocoonComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        _container.Insert(target, component.Cocoon);

        component.Prisoner = target;

        if (!HasComp<MutedComponent>(target))
            AddComp<MutedComponent>(target);
        else
            component.Mute = true;

        if (!HasComp<PacifiedComponent>(target))
            AddComp<PacifiedComponent>(target);
        else
            component.Pacified = true;

        if (!HasComp<TemporaryBlindnessComponent>(target))
            AddComp<TemporaryBlindnessComponent>(target);
        else
            component.Blindable = true;


        if (!component.IsHermetically)
            return;

        if (!HasComp<PressureImmunityComponent>(target))
            AddComp<PressureImmunityComponent>(target);
        else
            component.Pressure = true;
    }
}
