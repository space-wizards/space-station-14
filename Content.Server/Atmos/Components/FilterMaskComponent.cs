
namespace Content.Server.Atmos.Components;

/// <summary>
/// Used in head and mask with a smoke gas filter.
/// </summary>
[RegisterComponent]
[ComponentProtoName("FilterMask")]
public sealed partial class FilterMaskComponent : Component
{
    [DataField]
    public bool IsActive = false;
}

