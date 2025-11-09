using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Strip.Components;

/// <summary>
/// Give this to an entity when you want to decrease stripping times
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class ThievingComponent : Component
{
    /// <summary>
    /// How much the strip time should be shortened by
    /// </summary>
    // DS14-start
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("stripTimeReduction"), AutoNetworkedField]
    // DS14-end
    public TimeSpan StripTimeReduction = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// Should it notify the user if they're stripping a pocket?
    /// </summary>
    // DS14-start
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("stealthy"), AutoNetworkedField]
    // DS14-end
    public bool Stealthy;

    /// <summary>
    /// Variable pointing at the Alert modal
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> StealthyAlertProtoId = "Stealthy";

    /// <summary>
    /// Prevent component replication to clients other than the owner,
    /// doesn't affect prediction.
    /// Get mogged.
    /// </summary>
    public override bool SendOnlyToOwner => true;
}

/// <summary>
/// Event raised to toggle the thieving component.
/// </summary>
public sealed partial class ToggleThievingEvent : BaseAlertEvent;
