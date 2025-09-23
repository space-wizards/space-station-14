using Content.Shared.Atmos;

namespace Content.Shared.EntityEffects.Effects.Atmos;

// Server side system

[DataDefinition]
public sealed partial class CreateGas : EntityEffectBase<CreateGas>
{
    /// <summary>
    ///     The gas we're creating
    /// </summary>
    [DataField]
    public Gas Gas;

    /// <summary>
    ///     Mol modifier for gas creation.
    /// </summary>
    [DataField]
    public float Multiplier = 3f;
}
