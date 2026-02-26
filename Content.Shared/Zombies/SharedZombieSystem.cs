using Content.Shared.Armor;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Zombies;

public abstract class SharedZombieSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<ZombieComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnZombieStartup);

        SubscribeLocalEvent<ZombificationResistanceComponent, ArmorExamineEvent>(OnArmorExamine);
        SubscribeLocalEvent<ZombificationResistanceComponent, InventoryRelayedEvent<ZombificationResistanceQueryEvent>>(OnResistanceQuery);

        SubscribeLocalEvent<InitialInfectedComponent, ComponentStartup>(OnInitialInfectedStartup);
        SubscribeLocalEvent<InitialInfectedComponent, ComponentGetStateAttemptEvent>(OnInitialInfectedGetStateAttempt);
    }

    private void OnResistanceQuery(Entity<ZombificationResistanceComponent> ent, ref InventoryRelayedEvent<ZombificationResistanceQueryEvent> query)
    {
        query.Args.TotalCoefficient *= ent.Comp.ZombificationResistanceCoefficient;
    }

    private void OnArmorExamine(Entity<ZombificationResistanceComponent> ent, ref ArmorExamineEvent args)
    {
        var value = MathF.Round((1f - ent.Comp.ZombificationResistanceCoefficient) * 100, 1);

        if (value == 0)
            return;

        args.Msg.PushNewline();
        args.Msg.AddMarkupOrThrow(Loc.GetString(ent.Comp.Examine, ("value", value)));
    }

    private void OnRefreshSpeed(EntityUid uid, ZombieComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        var mod = component.ZombieMovementSpeedDebuff;
        args.ModifySpeed(mod, mod);
    }

    private void OnRefreshNameModifiers(Entity<ZombieComponent> entity, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("zombie-name-prefix");
    }

    private void OnInitialInfectedStartup(EntityUid uid, InitialInfectedComponent component, ComponentStartup args)
    {
        DirtyAllRelevant();
    }

    protected virtual void OnZombieStartup(EntityUid uid, ZombieComponent component, ComponentStartup args)
    {
        DirtyAllRelevant();
    }

    private void DirtyAllRelevant()
    {
        var initialInfectedQuery = EntityQueryEnumerator<InitialInfectedComponent>();
        while (initialInfectedQuery.MoveNext(out var uid, out var comp))
            Dirty(uid, comp);

        var zombieQuery = EntityQueryEnumerator<ZombieComponent>();
        while (zombieQuery.MoveNext(out var uid, out var comp))
            Dirty(uid, comp);
    }

    private void OnInitialInfectedGetStateAttempt(EntityUid uid, InitialInfectedComponent component, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Player?.AttachedEntity is not { } attached)
            return;

        if (HasComp<InitialInfectedComponent>(attached))
            return;

        if (HasComp<ZombieComponent>(attached))
            return;

        args.Cancelled = true;
    }
}
