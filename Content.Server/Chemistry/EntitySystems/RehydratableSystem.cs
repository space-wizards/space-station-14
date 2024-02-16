using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class RehydratableSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RehydratableComponent, SolutionContainerChangedEvent>(OnSolutionChange);
    }

    private void OnSolutionChange(Entity<RehydratableComponent> entity, ref SolutionContainerChangedEvent args)
    {
        var quantity = _solutions.GetTotalPrototypeQuantity(entity, entity.Comp.CatalystPrototype);
        if (quantity != FixedPoint2.Zero && quantity >= entity.Comp.CatalystMinimum)
        {
            Expand(entity);
        }
    }

    // Try not to make this public if you can help it.
    private void Expand(Entity<RehydratableComponent> entity)
    {
        var (uid, comp) = entity;

        _popups.PopupEntity(Loc.GetString("rehydratable-component-expands-message", ("owner", uid)), uid);

        var randomMob = _random.Pick(comp.PossibleSpawns);

        var target = Spawn(randomMob, Transform(uid).Coordinates);

        Transform(target).AttachToGridOrMap();
        var ev = new GotRehydratedEvent(target);
        RaiseLocalEvent(uid, ref ev);

        // prevent double hydration while queued
        RemComp<RehydratableComponent>(uid);
        QueueDel(uid);
    }
}
