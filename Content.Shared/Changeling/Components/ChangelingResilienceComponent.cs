using Content.Shared.Changeling.Systems;
using Content.Shared.Metabolism;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Component responsible for Changelings immunity to certain effects, such as revolutionary conversion, gibbing, or zombification.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedChangelingResilienceSystem))]
public sealed partial class ChangelingResilienceComponent : Component
{
    /// <summary>
    /// Prevents the changeling from being gibbed.
    /// Works by removing the GibBehaviour on init as well as cancelling gib attempt events.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PreventGibbing = true;

    /// <summary>
    /// Prevents the changeling from being converted to conversion antags, such as revolutionaries.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PreventConversion = true;

    /// <summary>
    /// Causes the organs of this changeling to be unable to turn into nymphs.
    /// Works by removing their respective components on init.
    /// </summary>
    [DataField]
    public bool PreventOrganNymphs = true;

    /// <summary>
    /// What metabolizer should be added to all organs on the owning entity.
    /// Happens on map init.
    /// </summary>
    [DataField]
    public ProtoId<MetabolizerTypePrototype>? AppendedMetabolizer = "Changeling";

    public override bool SendOnlyToOwner => true;
}
