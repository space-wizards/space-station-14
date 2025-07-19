using Content.Shared.Anomaly.Effects;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// On contact with an entity, if it meets the conditions, it will transfer the specified components to it
/// </summary>
[RegisterComponent, Access(typeof(SharedInnerBodyAnomalySystem))]
public sealed partial class InnerBodyAnomalyInjectorComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// components that will be automatically removed after “curing”
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry InjectionComponents = default!;
}
