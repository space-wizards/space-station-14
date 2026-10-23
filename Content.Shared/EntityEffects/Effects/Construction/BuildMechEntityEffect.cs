using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Construction;

public sealed partial class BuildMech : EntityEffectBase<BuildMech>
{
    [DataField(required: true)]
    public EntProtoId MechPrototype;

    [DataField]
    public string Container = "battery-container";
}
