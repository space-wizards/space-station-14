using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
namespace Content.Shared.Body.Part;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed class BodyCoveringComponent : Component
{
    [ViewVariables, DataField("primaryCoveringId", required:true, customTypeSerializer: typeof(PrototypeIdSerializer<BodyCoveringPrototype>))]
    public string PrimaryBodyCoveringId = string.Empty;

    [ViewVariables, DataField("secondaryCoveringId", required:false, customTypeSerializer: typeof(PrototypeIdSerializer<BodyCoveringPrototype>))]
    public string SecondaryBodyCoveringId = string.Empty;

    [ViewVariables, DataField("secondaryCoveringPercentage")]
    public float SecondaryCoveringPercentage = 0f;

    [ViewVariables, DataField("damageResistance", required:false)]
    public DamageModifierSet DamageResistance = new();
}
