using Content.Shared.Fluids.Components;

namespace Content.Shared.EntityEffects.Effects.Solution;

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Spill : EntityEffectBase<Spill>
{
    /// <summary>
    /// Optional fallback solution name if <see cref="SpillableComponent"/> is not present.
    /// </summary>
    [DataField]
    public string? Solution;
}
