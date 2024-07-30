using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

[Prototype("department")]
public sealed partial class DepartmentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    /// <summary>
    /// A description string to display in the character menu as an explanation of the department's function.
    /// </summary>
    [DataField(required: true)]
    public string Description = string.Empty;

    /// <summary>
    /// A color representing this department to use for text.
    /// </summary>
    [DataField(required: true)]
    public Color Color;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<JobPrototype>> Roles = new();

    /// <summary>
    /// Whether this is a primary department or not.
    /// For example, CE's primary department is engineering since Command has primary: false.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Primary = true;

    /// <summary>
    /// Departments with a higher weight sorted before other departments in UI.
    /// </summary>
    [DataField]
    public int Weight { get; private set; }

    /// <summary>
    /// Toggles the display of the department in the priority setting menu in the character editor.
    /// </summary>
    [DataField]
    public bool EditorHidden;
}

/// <summary>
/// Sorts <see cref="DepartmentPrototype"/> appropriately for display in the UI,
/// respecting their <see cref="DepartmentPrototype.Weight"/>.
/// </summary>
public sealed class DepartmentUIComparer : IComparer<DepartmentPrototype>
{
    public static readonly DepartmentUIComparer Instance = new();

    public int Compare(DepartmentPrototype? x, DepartmentPrototype? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (ReferenceEquals(null, y))
            return 1;

        if (ReferenceEquals(null, x))
            return -1;

        var cmp = -x.Weight.CompareTo(y.Weight);
        return cmp != 0 ? cmp : string.Compare(x.ID, y.ID, StringComparison.Ordinal);
    }
}
