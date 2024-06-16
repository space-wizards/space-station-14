using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.BarSign;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BarSignComponent : Component
{
    /// <summary>
    /// The current bar sign prototype being displayed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<BarSignPrototype>? Current;
}

[Serializable, NetSerializable]
public enum BarSignUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SetBarSignMessage(ProtoId<BarSignPrototype> sign) : BoundUserInterfaceMessage
{
    public ProtoId<BarSignPrototype> Sign = sign;
}
