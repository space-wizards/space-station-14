using Content.Shared.Clothing.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Allows this clothing item to transform along with the changeling so that it looks like whatever the mob we transform into is wearing in that slot.
/// Requires <see cref="ClothingComponent"/> and <see cref="ChameleonClothingComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingFleshClothingComponent : Component;
