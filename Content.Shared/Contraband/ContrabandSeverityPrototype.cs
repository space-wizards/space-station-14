using Robust.Shared.Prototypes;

namespace Content.Shared.Contraband;

/// <summary>
/// This is a prototype for defining the degree of severity for a particular <see cref="ContrabandComponent"/>
/// </summary>
[Prototype]
public sealed partial class ContrabandSeverityPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Text shown for this severity level when the contraband is examined.
    /// </summary>
    [DataField]
    public LocId ExamineText;

    /// <summary>
    /// When examining the contraband, should this take into account the viewer's departments and job?
    /// </summary>
    [DataField]
    public bool ShowDepartmentsAndJobs;
}
