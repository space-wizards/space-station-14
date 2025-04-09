using Content.Shared.Mind;
using Content.Shared.Teleportation.Components;
using Content.Shared.UserInterface;

namespace Content.Shared.Teleportation.Systems;

public abstract partial class SharedTeleportLocationsSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xForm = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportLocationsComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
        SubscribeLocalEvent<TeleportLocationsComponent, TeleportLocationRequestTeleportMessage>(OnTeleportLocationRequest);
    }

    private void OnUiOpenAttempt(Entity<TeleportLocationsComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        // TODO: If secondary use delay is active, return

        ent.Comp.ScrollOwner ??= args.User;
    }

    private void OnTeleportLocationRequest(Entity<TeleportLocationsComponent> ent, ref TeleportLocationRequestTeleportMessage args)
    {
        if (ent.Comp.ScrollOwner is null || !TryGetEntity(args.NetEnt, out var telePointEnt))
            return;

        _xForm.SetCoordinates(ent.Comp.ScrollOwner.Value, Transform(telePointEnt.Value).Coordinates);

        // TODO: After warp, emit smoke
        // TODO: On warp, announce warp point
    }
}
