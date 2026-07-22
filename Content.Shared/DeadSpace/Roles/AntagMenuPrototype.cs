// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.DeadSpace.Roles;

/// <summary>
/// Defines which antagonist categories are displayed and in which order.
/// </summary>
[Prototype("antagMenuCategory")]
public sealed partial class AntagMenuPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("list", required: true)]
    public List<ProtoId<AntagCategoryPrototype>> Categories { get; private set; } = new();
}

/// <summary>
/// A top-level expandable antagonist category.
/// </summary>
[Prototype]
public sealed partial class AntagCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("displayName", required: true)]
    public string Name { get; private set; } = string.Empty;

    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    [DataField]
    public Color OutlineColor { get; private set; } = Color.Red;

    [DataField]
    public List<ProtoId<AntagPrototype>> Antags { get; private set; } = new();

    [DataField]
    public List<AntagSubcategory> Subcategories { get; private set; } = new();
}

/// <summary>
/// An inline recursive category. Subcategories may be nested to any depth.
/// </summary>
[DataDefinition]
public sealed partial class AntagSubcategory
{
    [DataField("displayName", required: true)]
    public string Name { get; private set; } = string.Empty;

    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    /// <summary>
    /// Overrides the parent outline color. Null inherits the parent color.
    /// </summary>
    [DataField]
    public Color? OutlineColor { get; private set; }

    [DataField]
    public List<ProtoId<AntagPrototype>> Antags { get; private set; } = new();

    [DataField]
    public List<AntagSubcategory> Subcategories { get; private set; } = new();
}
