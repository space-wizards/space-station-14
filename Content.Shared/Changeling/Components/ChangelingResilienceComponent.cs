using Content.Shared.Changeling.Systems;
using Content.Shared.Metabolism;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Component responsible for Changelings immunity to certain effects, such as revolutionary conversion or gibbing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ChangelingResilienceSystem))]
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
    /// Removes these components from every organ on the owning entity.
    /// Happens only once on init.
    /// </summary>
    [DataField]
    public ComponentRegistry? OrganRemovedComponents;

    /// <summary>
    /// What metabolizer should be added to all organs on the owning entity.
    /// Happens on map init.
    /// </summary>
    [DataField]
    public ProtoId<MetabolizerTypePrototype>? AppendedMetabolizer = "Changeling";

    public override bool SendOnlyToOwner => true;
}
