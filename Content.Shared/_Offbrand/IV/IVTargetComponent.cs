using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.IV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IVSystem))]
public sealed partial class IVTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? IVSource;

    [DataField, AutoNetworkedField]
    public string? IVJointID;
}
