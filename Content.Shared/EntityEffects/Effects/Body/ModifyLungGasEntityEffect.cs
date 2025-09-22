using Content.Shared.Atmos;

namespace Content.Shared.EntityEffects.Effects.Body;

public sealed class ModifyLungGas : EntityEffectBase<ModifyLungGas>
{
    [DataField(required: true)]
    public Dictionary<Gas, float> Ratios = default!;
}
