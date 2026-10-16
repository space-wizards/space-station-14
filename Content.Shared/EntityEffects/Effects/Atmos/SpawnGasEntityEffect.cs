using Content.Shared.Atmos;

namespace Content.Shared.EntityEffects.Effects.Atmos;

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SpawnGas : EntityEffectBase<SpawnGas>
{
    /// <summary>
    /// The gas mixture to spawn.
    /// </summary>
    [DataField("gasMixture", required: true)]
    public GasMixture Gas = new();
}
