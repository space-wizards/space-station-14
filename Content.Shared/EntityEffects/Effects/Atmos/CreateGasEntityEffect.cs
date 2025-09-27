using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Atmos;

// Server side system

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

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var atmos = entSys.GetEntitySystem<SharedAtmosphereSystem>();
        var gasProto = atmos.GetGas(Gas);

        return Loc.GetString("entity-effect-guidebook-create-gas",
            ("chance", Probability),
            ("moles", Multiplier),
            ("gas", gasProto.Name));
    }
}
