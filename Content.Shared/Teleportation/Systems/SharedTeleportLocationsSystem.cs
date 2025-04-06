using Content.Shared.Teleportation.Components;
using Content.Shared.UserInterface;

namespace Content.Shared.Teleportation.Systems;

public abstract partial class SharedTeleportLocationsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportLocationsComponent, AfterActivatableUIOpenEvent>(OnTeleportLocationsOpen);
    }

    protected virtual void OnTeleportLocationsOpen(Entity<TeleportLocationsComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        // TODO: Probably in the before method, check if the secondary use delay is active. If it is, either disable all buttons or prevent menu from being open.
    }

    // TODO: After warp, emit smoke
    // TODO: On warp, announce warp point
}
