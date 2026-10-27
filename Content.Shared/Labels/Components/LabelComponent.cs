using Content.Shared.Labels.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Labels.Components;

/// <summary>
/// Makes entities have a label in their name. Labels are normally given by <see cref="HandLabelerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(LabelSystem))]
public sealed partial class LabelComponent : Component
{
    /// <summary>
    /// Current text on the label.
    /// Do not use this in entity prototypes - use <see cref="LocalizedLabel"/> instead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? CurrentLabel { get; set; }

    /// <summary>
    /// Localization ID used to set <see cref="CurrentLabel"/> on map init.
    /// Use this in entity prototypes.
    /// </summary>
    [DataField]
    public LocId? LocalizedLabel { get; set; }

    /// <summary>
    /// Should the label show up in the examine menu?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Examinable = true;
}
