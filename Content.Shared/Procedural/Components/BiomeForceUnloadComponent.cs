using Robust.Shared.GameStates;

namespace Content.Shared.Procedural.Components;

/// <summary>
/// Will forcibly unload an entity no matter what. Useful if you have consistent entities that will never be default or the likes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BiomeForceUnloadComponent : Component;
