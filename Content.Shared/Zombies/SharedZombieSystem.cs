using Content.Shared.Armor;
using Content.Shared.Antag;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared.Zombies;

public abstract class SharedZombieSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<ZombieComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<ZombificationResistanceComponent, ArmorExamineEvent>(OnArmorExamine);
        SubscribeLocalEvent<ZombificationResistanceComponent, InventoryRelayedEvent<ZombificationResistanceQueryEvent>>(OnResistanceQuery);
        SubscribeLocalEvent<PendingZombieComponent, ComponentGetStateAttemptEvent>(OnZombieCompGetStateAttempt);
        SubscribeLocalEvent<InitialInfectedComponent, ComponentGetStateAttemptEvent>(OnZombieCompGetStateAttempt);
    }

    /// <summary>
    /// Determines if a zombie antag component should be sent to a client.
    /// Only the infected player themselves and observers with ShowAntagIconsComponent receive it.
    /// </summary>
    private void OnZombieCompGetStateAttempt(EntityUid uid, Component comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(uid, args.Player);
    }

    /// <summary>
    /// Returns true if the player should receive the zombie antag component state.
    /// </summary>
    private bool CanGetState(EntityUid uid, ICommonSession? player)
    {
        // Allow in replays.
        if (player?.AttachedEntity is not { } attachedUid)
            return true;

        // The infected player can always see their own component.
        if (attachedUid == uid)
            return true;

        // Observers/admins with ShowAntagIconsComponent can see it (e.g. ghost mode).
        if (HasComp<ShowAntagIconsComponent>(attachedUid))
            return true;

        return false;
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
}

