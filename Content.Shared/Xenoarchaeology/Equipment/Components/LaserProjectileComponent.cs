using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
/// Marker component to indicate that entity is a laser projectile (focused beam of high energy photons).
/// </summary>
/// <remarks> Is used by XenoArcheology as way to whitelist some hitscans for trigger. </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class LaserProjectileComponent : Component;
