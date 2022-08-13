using System.Collections.Immutable;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeComputer;

[Serializable, NetSerializable]
public abstract class CartridgeComputerUIState : BoundUserInterfaceState
{
    public EntityUid? ActiveUI;
    public IReadOnlyList<EntityUid> InstalledPrograms = ImmutableList<EntityUid>.Empty;
}
