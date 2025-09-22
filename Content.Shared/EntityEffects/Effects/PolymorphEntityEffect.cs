using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;
public sealed class Polymorph : EntityEffectBase<Polymorph>
{
    /// <summary>
    ///     What polymorph prototype is used on effect
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Prototype;
}
