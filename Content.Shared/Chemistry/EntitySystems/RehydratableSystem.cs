using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed class RehydratableSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RehydratableComponent, SolutionContainerChangedEvent>(OnSolutionChange);
    }

    private void OnSolutionChange(Entity<RehydratableComponent> ent, ref SolutionContainerChangedEvent args)
    {
        var quantity = _solutions.GetTotalPrototypeQuantity(ent, ent.Comp.CatalystPrototype);
        if (quantity != FixedPoint2.Zero && quantity >= ent.Comp.CatalystMinimum)
        {
            Expand(ent);
        }
    }

    // Try not to make this public if you can help it.
    private void Expand(Entity<RehydratableComponent> ent)
    {
        var (uid, comp) = ent;

        var randomMob = _random.Pick(comp.PossibleSpawns);

        var target = Spawn(randomMob, Transform(uid).Coordinates);
        _popup.PopupEntity(Loc.GetString("rehydratable-component-expands-message", ("owner", uid)), target);

        _xform.AttachToGridOrMap(target);
        var ev = new GotRehydratedEvent(target);
        RaiseLocalEvent(uid, ref ev);

        // prevent double hydration while queued
        RemComp<RehydratableComponent>(uid);
        QueueDel(uid);
    }
}
