using System.Diagnostics.CodeAnalysis;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;

namespace Content.Client.Storage.Systems;

public sealed class EntityStorageSystem : SharedEntityStorageSystem
{
    public override bool ResolveStorage(EntityUid uid, [NotNullWhen(true)] ref SharedEntityStorageComponent? component)
    {
        return Resolve(uid, ref component);
    }
}
