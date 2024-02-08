using Content.Server.Body.Systems;
namespace Content.Server.Atmos.Components;

/// <summary>
/// Used in head and mask with a smoke gas filter.
/// </summary>
[RegisterComponent, Access(typeof(SmokeFilterSystem))]
public sealed partial class FilterMaskComponent : Component
{
    [DataField]
    public bool IsActive = false;
}

