using Content.Shared.Renameable.Systems;
using Robust.Shared.Players;

namespace Content.Client.Renameable.Systems;

public sealed class RenameableSystem : SharedRenameableSystem
{
    protected override bool TryOpen(EntityUid uid, EntityUid user)
    {
        return true;
    }
}
