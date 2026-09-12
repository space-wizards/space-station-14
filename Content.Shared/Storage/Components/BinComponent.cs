using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Storage.Components;

/// <summary>
/// This is used for things like paper bins, in which
/// you can only take off of the top of the bin.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BinSystem))]
public sealed partial class BinComponent : Component
{
    /// <summary>
    /// The containers that contain the items held in the bin
    /// </summary>
    [ViewVariables]
    public Container ItemContainer = default!;

    /// <summary>
    /// ID of the container used to hold the items in the bin.
    /// </summary>
    [DataField]
    public string ContainerId = "bin-container";

    /// <summary>
    /// Examine string that displays when there are items in the bin.
    /// </summary>
    [DataField]
    public string ExamineText = "bin-component-on-examine-text";

    /// <summary>
    /// Examine text that displays when the bin is empty.
    /// </summary>
    [DataField]
    public string EmptyText = "bin-component-on-examine-empty-text";

    /// <summary>
    /// Icon for insertion
    /// </summary>
    [DataField]
    public SpriteSpecifier? InsertIcon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/in.svg.192dpi.png"));

    /// <summary>
    /// Icon for removal.
    /// </summary>
    [DataField]
    public SpriteSpecifier? RemoveIcon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"));

    /// <summary>
    /// A whitelist governing what items can be inserted into the bin.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The maximum amount of items
    /// that can be stored in the bin.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxItems = 20;
}
