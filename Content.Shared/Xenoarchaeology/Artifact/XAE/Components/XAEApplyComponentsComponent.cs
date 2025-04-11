using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Applies components when effect is activated.
/// </summary>
[RegisterComponent, Access(typeof(XAEApplyComponentsSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XAEApplyComponentsComponent : Component
{
    /// <summary>
    /// Components that are permanently added to an entity when the effect's node is entered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Does adding components need to be done only on first activation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ApplyIfAlreadyHave { get; set; }

    /// <summary>
    /// Does component need to be restored when activated 2nd or more times.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RefreshOnReactivate { get; set; }
}
