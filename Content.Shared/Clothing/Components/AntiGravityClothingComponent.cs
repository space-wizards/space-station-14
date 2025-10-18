using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// This is used for clothing that makes an entity weightless when worn.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AntiGravityClothingComponent : Component;
