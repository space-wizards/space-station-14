using Content.Shared.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.Equipment.Components;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

/// <inheritdoc cref="SharedNodeScannerSystem"/>
public sealed class NodeScannerSystem : SharedNodeScannerSystem
{
    protected override void TryOpenUi(Entity<NodeScannerComponent> device, EntityUid actor)
    {
        // no-op
    }
}
