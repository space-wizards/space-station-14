using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc;

/// <summary>
/// Added to entities tethered by a telekinesis.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class COTelekinesisTetherComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Tetherer;

    [ViewVariables, DataField, AutoNetworkedField]
    public float OriginalAngularDamping;
}
