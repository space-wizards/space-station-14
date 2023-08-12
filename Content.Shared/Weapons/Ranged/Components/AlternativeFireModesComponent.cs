using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Linq;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class AlternativeFireModesComponent : Component
{
    [DataField("fireModes")]
    [AutoNetworkedField]
    public List<FireMode> FireModes = new();

    [AutoNetworkedField]
    public int CurrentFireModeIndex = 0;

    public FireMode? CurrentFireMode
    {
        get
        {
            if (FireModes == null || !FireModes.Any())
                return null;

            return FireModes.ElementAtOrDefault(CurrentFireModeIndex);
        }
    }
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed class FireMode
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("fireCost")]
    public float FireCost = 100;
}
