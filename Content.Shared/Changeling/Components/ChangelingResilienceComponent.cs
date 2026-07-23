using Content.Shared.Changeling.Systems;
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

    public override bool SendOnlyToOwner => true;
}
