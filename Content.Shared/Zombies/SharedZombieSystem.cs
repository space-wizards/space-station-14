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

    private void OnInitialInfectedStartup(Entity<InitialInfectedComponent> ent, ref ComponentStartup args)
    {
        DirtyInitialInfected();
    }

    protected virtual void OnZombieStartup(Entity<ZombieComponent> ent, ref ComponentStartup args)
    {
        DirtyInitialInfected();
    }

    /// <summary>
    /// Forces a network state update for all <see cref="InitialInfectedComponent"/>.
    /// Ensures that clients entitled to see this component actually receive it when a new initial infected appears.
    /// </summary>
    /// <remarks>
    /// TODO: This is a temporary solution until a more efficient targeted dirtying mechanism is available.
    /// </remarks>
    private void DirtyInitialInfected()
    {
        var initialInfectedQuery = EntityQueryEnumerator<InitialInfectedComponent>();
        while (initialInfectedQuery.MoveNext(out var uid, out var comp))
            Dirty(uid, comp);
    }

    /// <summary>
    /// Restricts <see cref="InitialInfectedComponent"/> state to entities that are themselves initial infected or zombies.
    /// </summary>
    private void OnInitialInfectedGetStateAttempt(Entity<InitialInfectedComponent> ent, ref ComponentGetStateAttemptEvent args)
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
