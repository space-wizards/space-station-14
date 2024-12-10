using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Content.Shared.Examine;
using Content.Shared.Kitchen.Components;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed class RehydratableSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RehydratableComponent, SolutionContainerChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<RehydratableComponent, ExaminedEvent>(OnExamine);
		SubscribeLocalEvent<RehydratableComponent, BeingMicrowavedEvent>(OnMicrowaved);
    }

    private void OnSolutionChange(Entity<RehydratableComponent> ent, ref SolutionContainerChangedEvent args)
    {
        var quantity = _solutions.GetTotalPrototypeQuantity(ent, ent.Comp.CatalystPrototype);
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner)} was hydrated, now contains a solution of: {SharedSolutionContainerSystem.ToPrettyString(args.Solution)}.");
        if (quantity != FixedPoint2.Zero && quantity >= ent.Comp.CatalystMinimum)
        {
            Expand(ent);
        }
    }

	private void OnMicrowaved(EntityUid uid, RehydratableComponent component, BeingMicrowavedEvent args)
    {
		if(args.Time <= 0)
			return;
		if (!_solutions.TryGetSolution(uid, component.SolutionName, out _, out var solution))
            return;
        solution.RemoveAllSolution();
    }

    private void OnExamine(EntityUid uid, RehydratableComponent component, ExaminedEvent args)
    {
        if (!_solutions.TryGetSolution(uid, component.SolutionName, out _, out var solution))
            return;
        // This will only be true if an incorrect reagent has been added to the cube
        if (solution.Volume == solution.MaxVolume)
        {
            args.PushMarkup(Loc.GetString("rehydratable-component-soaked-message"));
        }
    }

    // Try not to make this public if you can help it.
    private void Expand(Entity<RehydratableComponent> ent)
    {
        if (_net.IsClient)
            return;

        var (uid, comp) = ent;

        var randomMob = _random.Pick(comp.PossibleSpawns);

        var target = Spawn(randomMob, Transform(uid).Coordinates);
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner)} has been hydrated correctly and spawned: {ToPrettyString(target)}.");

        _popup.PopupEntity(Loc.GetString("rehydratable-component-expands-message", ("owner", uid)), target);

        _xform.AttachToGridOrMap(target);
        var ev = new GotRehydratedEvent(target);
        RaiseLocalEvent(uid, ref ev);

        // prevent double hydration while queued
        RemComp<RehydratableComponent>(uid);
        QueueDel(uid);
    }
}
