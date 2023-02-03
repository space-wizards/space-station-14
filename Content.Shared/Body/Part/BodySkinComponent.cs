using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Body.Part;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed class BodySkinComponent : Component
{
    [Access(typeof(WoundSystem), typeof(SharedBodySystem), Other = AccessPermissions.Read)] [DataField("skinLayers")]
    public List<SkinlayerData> SkinLayers = new();

    public SkinlayerData? PrimaryLayer
    {
        get
        {
            if (SkinLayers.Count > 0)
                return SkinLayers[0];
            return null;
        }
    }
}

[Serializable, NetSerializable, DataRecord]
public record struct SkinlayerData(
    [field: DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<BodyCoveringPrototype>))]
    string? ProtoType,
    [field: DataField("name", required: true)]
    string Name,
    string Description, float Coverage = 1.0f,
    DamageModifierSet? Resistance = null);
