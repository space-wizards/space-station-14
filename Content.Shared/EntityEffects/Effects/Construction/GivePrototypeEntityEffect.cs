using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Construction;

public sealed partial class GivePrototype : EntityEffectBase<GivePrototype>
{
    [DataField(required:true)]
    public EntProtoId Prototype;

    [DataField]
    public int Amount = 1;
}
