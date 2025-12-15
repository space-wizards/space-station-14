using Content.Shared.Administration.Managers.Bwoink.Features;
using JetBrains.Annotations;

namespace Content.Shared.Administration.Managers.Bwoink;

/// <summary>
/// Defines how requirements should be executed.
/// </summary>
public enum RequirementOperationMode
{
    /// <summary>
    /// All conditions must pass.
    /// </summary>
    All,
    /// <summary>
    /// Any of condition must pass.
    /// </summary>
    Any,
    /// <summary>
    /// If any conditions pass, the requirement fails.
    /// </summary>
    InvertedAny,
    /// <summary>
    /// Conditions are not evaluated at all, always deny.
    /// </summary>
    None,
}

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class BwoinkChannelCondition;

/// <summary>
/// Holder for conditions for a bwoink channel.
/// </summary>
[DataDefinition]
public sealed partial class BwoinkChannelRequirement
{
    [DataField]
    public RequirementOperationMode OperationMode { get; set; } = RequirementOperationMode.All;

    [DataField(required:true)]
    public List<BwoinkChannelCondition> Requirements { get; set; }
}
