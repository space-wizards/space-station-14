namespace Content.Shared.Defects.Components;

/// <summary>
/// A simple defect marker with no runtime behavior.
/// Used on items with fixed (non-randomized-value) defects so they participate
/// in the naming and description system.
/// </summary>
[RegisterComponent]
public sealed partial class SimpleDefectComponent : DefectComponent
{
}
