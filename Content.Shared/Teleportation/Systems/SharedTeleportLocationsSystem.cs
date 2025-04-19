using Content.Shared.Teleportation.Components;
using Content.Shared.Timing;
using Content.Shared.UserInterface;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// <inheritdoc cref="TeleportLocationsComponent"/>
/// </summary>
public abstract partial class SharedTeleportLocationsSystem : EntitySystem
{
    [Dependency] protected readonly UseDelaySystem Delay = default!;

    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    protected const string TeleportDelay = "TeleportDelay";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportLocationsComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
        SubscribeLocalEvent<TeleportLocationsComponent, AfterActivatableUIOpenEvent>(OnUiOpen);
        SubscribeLocalEvent<TeleportLocationsComponent, TeleportLocationDestinationMessage>(OnTeleportToLocationRequest);
    }

    private void OnUiOpen(Entity<TeleportLocationsComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        ent.Comp.User = args.User;
    }

    private void OnUiOpenAttempt(Entity<TeleportLocationsComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!Delay.IsDelayed(ent.Owner, TeleportDelay))
            return;

        args.Cancel();
    }

    protected virtual void OnTeleportToLocationRequest(Entity<TeleportLocationsComponent> ent, ref TeleportLocationDestinationMessage args)
    {
        if (ent.Comp.User is null || !TryGetEntity(args.NetEnt, out var telePointEnt) || Delay.IsDelayed(ent.Owner, TeleportDelay))
            return;

        var comp = ent.Comp;
        var originEnt = comp.User.Value;
        var destination = Transform(telePointEnt.Value).Coordinates;

        SpawnAtPosition(comp.TeleportEffect, Transform(originEnt).Coordinates);

        _xform.SetCoordinates(originEnt, destination);

        SpawnAtPosition(comp.TeleportEffect, destination);

        Delay.TryResetDelay(ent.Owner, true, id: TeleportDelay);

        if (!ent.Comp.CloseAfterTeleport)
            return;

        // Teleport's done, now tell the BUI to close if needed.
        _ui.CloseUi(ent.Owner, TeleportLocationUiKey.Key);
    }
}
