namespace Content.Shared.Guidebook;

/// <summary>
/// Indicates that GuidebookDataSystem should include this field/property when
/// scanning entity prototypes for values to extract.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class GuidebookDataAttribute : Attribute { }
