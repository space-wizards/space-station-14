using Robust.Shared.Containers;

namespace Content.Shared.Implants.Components;

/// <summary>
/// Added to an entity via the <see cref="SharedImplanterSystem"/> on implant
/// Used in instances where mob info needs to be passed to the implant such as MobState triggers
/// </summary>
[RegisterComponent]
public sealed class ImplantedComponent : Component
{
    public Container ImplantContainer = default!;
}
