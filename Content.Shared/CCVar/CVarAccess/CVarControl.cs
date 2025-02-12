using Content.Shared.Administration;
using Robust.Shared.Reflection;

namespace Content.Shared.CCVar.CVarAccess;

/// <summary>
/// Manages what admin flags can change the cvar value. With optional mins and maxes.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[Reflect(discoverable: true)]
public sealed class CVarControl : Attribute
{
    public AdminFlags AdminFlags { get; }
    public object? Min { get; }
    public object? Max { get; }

    public CVarControl(AdminFlags adminFlags, object? min = null, object? max = null)
    {
        AdminFlags = adminFlags;
        Min = min;
        Max = max;
    }
}
