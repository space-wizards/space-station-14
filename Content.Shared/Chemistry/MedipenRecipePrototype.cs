
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Chemistry;

[NetSerializable, Serializable, Prototype("medipenRecipe")]
public sealed partial class MedipenRecipePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("result", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? Result { get; set; }

    [DataField("reagents", required: true)]
    public Dictionary<string, int> ReagentsRequired { get; set; } = new();
}
