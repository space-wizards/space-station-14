namespace Content.Shared.Guidebook;

/// <summary>
/// Indicates that GuidebookDataSystem should include this field/property when
/// scanning entity prototypes for values to extract.
/// </summary>
/// <remarks>
/// Note that this will not work for client-only components, because the data extraction
/// is done on the server (it uses reflection, which is blocked by the sandbox on clients).
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class GuidebookDataAttribute : Attribute { }
