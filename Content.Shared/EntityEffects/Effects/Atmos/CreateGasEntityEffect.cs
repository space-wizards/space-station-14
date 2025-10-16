using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Atmos;

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class CreateGas : EntityEffectBase<CreateGas>
{
    /// <summary>
    ///     The gas we're creating
    /// </summary>
    [DataField]
    public Gas Gas;

    /// <summary>
    ///     Amount of moles we're creating
    /// </summary>
    [DataField]
    public float Moles = 3f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var atmos = entSys.GetEntitySystem<SharedAtmosphereSystem>();
        var gasProto = atmos.GetGas(Gas);

        return Loc.GetString("entity-effect-guidebook-create-gas",
            ("chance", Probability),
            ("moles", Moles),
            ("gas", gasProto.Name));
    }
}
