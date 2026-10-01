using Content.Shared.Temperature.Systems;

namespace Content.Server.Temperature.Components;

/// <summary>
/// This component allows heat containers within an entity to be connected to each other and or the outside world.
/// It is meant to generalize thermodynamics within entity trees thus the name, with all optimisations being made in one central place.
/// </summary>
[RegisterComponent]
public sealed partial class GeneralThermodynamicsComponent : Component
{
    [DataDefinition]
    public sealed partial class TwoWay
    {
        public required HeatContainerQuerySystem.HeatContainerAddress AddressA { get; set; }

        public required HeatContainerQuerySystem.HeatContainerAddress AddressB { get; set; }

        public float Conductivity { get; set; }
    }

    [DataDefinition]
    public sealed partial class OneWay
    {
        public required HeatContainerQuerySystem.HeatContainerAddress AddressA { get; set; }

        public float? Conductivity { get; set; }
    }

    /// <summary>
    /// Heat containers exposed to air
    /// </summary>
    [DataField]
    public List<OneWay> Exposures { get; set; } = [];

    /// <summary>
    /// Heat containers in a container slot which should slip
    /// </summary>
    [DataField]
    public List<OneWay> SelfMix { get; set; } = [];

    /// <summary>
    /// Heat Containers linked with one another.
    /// </summary>
    [DataField]
    public List<TwoWay> Connections { get; set; } = [];
}
