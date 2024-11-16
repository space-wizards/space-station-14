using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Applies components when effect is activated.
/// </summary>
[RegisterComponent, Access(typeof(XAEApplyComponentsSystem))]
public sealed partial class XAEApplyComponentsComponent : Component
{
    /// <summary>
    /// Components that are permanently added to an entity when the effect's node is entered.
    /// </summary>
    [DataField]
    public ComponentRegistry PermanentComponents = new();
}
