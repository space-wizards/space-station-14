using Content.Shared.Conduit;
using Content.Shared.Conduit.Holder;
using Robust.Server.GameStates;

namespace Content.Server.Conduit;

/// <inheritdoc/>
public sealed partial class ConduitSystem : SharedConduitSystem
{
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;

    /// <inheritdoc/>
    protected override void AddPVSOverride(Entity<ConduitHolderComponent> ent)
    {
        _pvs.AddGlobalOverride(ent);
    }
}
