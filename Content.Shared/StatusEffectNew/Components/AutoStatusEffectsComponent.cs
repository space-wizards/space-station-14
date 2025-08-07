namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// Marker component for all status effects - every status effect entity should have it.
/// Provides a link between the effect and the affected entity, and some data common to all status effects.
/// </summary>
[RegisterComponent]
[Access(typeof(StatusEffectsSystem))]
public sealed partial class AutoStatusEffectsComponent : Component
{
}
