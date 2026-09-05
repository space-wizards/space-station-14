using Content.Shared.Temperature.Systems;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// A simple static component to tell outside entities which heat containers they might access when queries by <see cref="HeatContainerQuerySystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class HeatableComponent : Component
{
    [DataField]
    public HeatContainerQuerySystem.HeatContainerAddress[] ExposedContainers { get; set; } = [];
}
