using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

[DataDefinition]
public sealed partial class Polymorph : EntityEffectBase<Polymorph>
{
    /// <summary>
    ///     What polymorph prototype is used on effect
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Prototype;
}
