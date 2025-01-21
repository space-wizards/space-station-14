using Content.Shared.ParcelWrap.Components;
using Content.Shared.ParcelWrap.Systems;

namespace Content.Client.ParcelWrap;

/// <inheritdoc/>
public sealed class ParcelWrappingSystem : SharedParcelWrappingSystem
{
    // Do not spawn anything on the client.
    protected override Entity<WrappedParcelComponent>? SpawnParcelAndInsertTarget(EntityUid user,
        Entity<ParcelWrapComponent> wrapper,
        EntityUid target)
    {
        return null;
    }

    // Do not spawn anything on the client.
    protected override void SpawnUnwrapTrash(Entity<WrappedParcelComponent, TransformComponent> parcel) { }
}
