using System.Collections.Immutable;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader;

[Virtual]
[Serializable, NetSerializable]
public class CartridgeLoaderUiState : BoundUserInterfaceState
{
    public EntityUid? ActiveUI;
    public List<EntityUid> Programs = new();
}
