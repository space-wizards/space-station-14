using Content.Server.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class RehydratableSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RehydratableComponent, SolutionChangedEvent>(OnSolutionChange);
    }

    private void OnSolutionChange(EntityUid uid, RehydratableComponent comp, SolutionChangedEvent args)
    {
        var quantity = _solutions.GetReagentQuantity(uid, comp.CatalystPrototype);
        if (quantity != FixedPoint2.Zero && quantity >= comp.CatalystMinimum)
        {
            Expand(uid, comp);
        }
    }

    // Try not to make this public if you can help it.
    private void Expand(EntityUid uid, RehydratableComponent comp)
    {
        _popups.PopupEntity(Loc.GetString("rehydratable-component-expands-message", ("owner", uid)), uid);

        var target = Spawn(comp.TargetPrototype, Transform(uid).Coordinates);
        Transform(target).AttachToGridOrMap();
        var ev = new GotRehydratedEvent(target);
        RaiseLocalEvent(uid, ref ev);

        // prevent double hydration while queued
        RemComp<RehydratableComponent>(uid);
        QueueDel(uid);
    }
}
