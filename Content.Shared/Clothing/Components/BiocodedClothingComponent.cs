using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     Restrict wearing this clothing for everyone, except owner.
///     First person that equipped clothing is saved as clothing owner.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BiocodedClothingComponent : Component
{
    /// <summary>
    ///     If biocoding enabled? Can be toggled by verb.
    /// </summary>
    [DataField("enabled")]
    [AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    ///     Person that currently wear this clothing.
    ///     Null if no one wearing it.
    /// </summary>
    [DataField("wearer")]
    [AutoNetworkedField]
    public EntityUid? CurrentWearer;

    /// <summary>
    ///     Entity that is owner of biocoded item. Null if no owner.
    /// </summary>
    /// <remarks>
    ///     If owner was cloned, this item will not recognize them.
    /// </remarks>
    [DataField("biocodedOwner")]
    [AutoNetworkedField]
    public EntityUid? BiocodedOwner;
}
