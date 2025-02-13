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

    public CVarControl(AdminFlags adminFlags, object? min = null, object? max = null, string? helpText = null)
    {
        AdminFlags = adminFlags;
        Min = min;
        Max = max;

        // Not actually sure if its a good idea to throw exceptions in attributes.

        if (min != null && max != null)
        {
            if (min.GetType() != max.GetType())
            {
                throw new ArgumentException("Min and max must be of the same type.");
            }
        }

        if (min == null && max != null || min != null && max == null)
        {
            throw new ArgumentException("Min and max must both be null or both be set.");
        }
    }
}
