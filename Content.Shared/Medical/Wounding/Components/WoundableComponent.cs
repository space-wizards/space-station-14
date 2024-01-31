using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Prototypes;
using Content.Shared.Medical.Wounding.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Wounding.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(WoundSystem))]
public sealed partial class WoundableComponent : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Body;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? RootWoundable;

    public const string WoundableContainerId = "Wounds";

    /// <summary>
    /// Should we spread damage to child and parent woundables when we gib this part
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SplatterDamageOnDestroy = true;

    [DataField, AutoNetworkedField]
    public EntProtoId? AmputationWoundProto = null;

    [DataField(required:true, customTypeSerializer: typeof(PrototypeIdDictionarySerializer<WoundingMetadata,DamageTypePrototype>)), AutoNetworkedField]
    public Dictionary<string, WoundingMetadata> Config = new();

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Health = -1; //this is set during comp init or overriden when defined

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 HealthCap = -1; //this is set during comp init

    [DataField(required: true),ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaxHealth = 50;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Integrity = -1; //this is set during comp init or overriden when defined

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 IntegrityCap = -1; //this is set during comp init

    [DataField(required: true),ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaxIntegrity = 10;

    public FixedPoint2 HitPoints => Health + Integrity;

}
