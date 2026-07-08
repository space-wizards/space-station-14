using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed partial class RehydratableSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RehydratableComponent, SolutionChangedEvent>(OnSolutionChange);
    }

    private void OnSolutionChange(Entity<RehydratableComponent> ent, ref SolutionChangedEvent args)
    {
        // The changes are already networked as part of the same game state.
        if (_timing.ApplyingState)
            return;

        var quantity = _solutions.GetTotalPrototypeQuantity(ent.Owner, ent.Comp.CatalystPrototype);
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner)} was hydrated, now contains a solution of: {SharedSolutionContainerSystem.ToPrettyString(args.Solution.Comp.Solution)}.");
        if (quantity != FixedPoint2.Zero && quantity >= ent.Comp.CatalystMinimum)
        {
            Expand(ent);
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
