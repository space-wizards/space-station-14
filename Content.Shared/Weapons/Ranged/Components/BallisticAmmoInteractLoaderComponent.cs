namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// If an entity with <see cref="BallisticAmmoProviderComponent"/> has this component, it can be used to interact
/// with the ammo entity to load it into the gun (or magazine).
/// Basically the reverse order (used vs target) to achieve the same result (loading the gun)
/// </summary>
[RegisterComponent]
public sealed partial class BallisticAmmoInteractLoaderComponent : Component;
