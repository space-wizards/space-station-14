using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Power.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PoweredLockerComponent : Component
{
    /// <summary>
    /// Whether or not the locker is powered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Powered = true;
}