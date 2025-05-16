using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class CreateGas : EventEntityEffect<CreateGas>
{
    [DataField(required: true)]
    public Gas Gas = default!;

    /// <summary>
    ///     For each unit consumed, how many moles of gas should be created?
    /// </summary>
    [DataField]
    public float Multiplier = 3f;

    public override bool ShouldLog => true;
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var atmos = entSys.GetEntitySystem<SharedAtmosphereSystem>();
        var gasProto = atmos.GetGas(Gas);

        return Loc.GetString("reagent-effect-guidebook-create-gas",
            ("chance", Probability),
            ("moles", Multiplier),
            ("gas", gasProto.Name));
    }

    public override LogImpact LogImpact => LogImpact.High;
}
