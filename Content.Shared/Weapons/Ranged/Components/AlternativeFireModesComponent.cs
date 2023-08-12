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

    private int _currentFireModeIndex = 0;

    [AutoNetworkedField]
    public int CurrentFireModeIndex
    {
        get { return _currentFireModeIndex; }

        set
        {
            if (value >= FireModes.Count)
            {
                _currentFireModeIndex = 0;
            }

            else if (value < 0)
            {
                _currentFireModeIndex = FireModes.Count - 1;
            }

            else
            {
                _currentFireModeIndex = value;
            }
        }
    }

    public FireMode? CurrentFireMode
    {
        get
        {
            if (FireModes == null || !FireModes.Any())
                return null;

            return FireModes[CurrentFireModeIndex];
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
