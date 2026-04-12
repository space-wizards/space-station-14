namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// A simple marker component for a <see cref="StatusEffectComponent"/> which allows this status effect to be cloned
/// by the CloningSystem (for example for paradox clones, cloning pods or changeling transformations).
/// This is used for traits that use permanent status effects.
/// </summary>
[RegisterComponent]
public sealed partial class CloneableStatusEffectComponent : Component;
