using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;
using Robust.Server.GameStates;

namespace Content.Server.Disposal.Tube;

/// <inheritdoc/>
public sealed partial class DisposalTubeSystem : SharedDisposalTubeSystem
{
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;

    /// <inheritdoc/>
    protected override void AddPVSOverride(Entity<DisposalHolderComponent> ent)
    {
        _pvs.AddGlobalOverride(ent);
    }
}
