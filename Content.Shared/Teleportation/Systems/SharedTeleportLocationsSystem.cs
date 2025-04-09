using Content.Shared.Teleportation.Components;
using Content.Shared.Timing;
using Content.Shared.UserInterface;

namespace Content.Shared.Teleportation.Systems;

public abstract partial class SharedTeleportLocationsSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xForm = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uI = default!;

    private const string TeleportDelay = "TeleportDelay";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportLocationsComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
        SubscribeLocalEvent<TeleportLocationsComponent, TeleportLocationRequestTeleportMessage>(OnTeleportLocationRequest);
    }

    private void OnUiOpenAttempt(Entity<TeleportLocationsComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (_delay.IsDelayed(ent.Owner, TeleportDelay))
            return;

        ent.Comp.User ??= args.User;
    }

    protected virtual void OnTeleportLocationRequest(Entity<TeleportLocationsComponent> ent, ref TeleportLocationRequestTeleportMessage args)
    {
        if (ent.Comp.User is null || !TryGetEntity(args.NetEnt, out var telePointEnt))
            return;

        var comp = ent.Comp;
        var originEnt = comp.User.Value;
        var destination = Transform(telePointEnt.Value).Coordinates;

        SpawnAtPosition(comp.TeleportEffect, Transform(originEnt).Coordinates);

        _xForm.SetCoordinates(originEnt, destination);

        SpawnAtPosition(comp.TeleportEffect, destination);

        _delay.TryResetDelay(ent.Owner, true, id: TeleportDelay);

        if (!ent.Comp.CloseAfterTeleport)
            return;

        // Teleport's done, now tell the BUI to close if needed.
        _uI.ServerSendUiMessage(ent.Owner, TeleportLocationUiKey.Key, new TeleportLocationRequestCloseMessage());
    }
}
