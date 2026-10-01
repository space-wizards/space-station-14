using Content.Shared.Chemistry.Components;

namespace Content.Shared.Temperature.HeatContainer;

public record struct BoxedHeatContainer(EntityUid EntityUid, IComponent Component, float HeatCapacity, float Temperature) : IHeatContainer
{
    public float HeatCapacity { get; set; } = HeatCapacity;
    public float Temperature { get; set; } = Temperature;

    public IComponent Component { get; } = Component;

    public EntityUid EntityUid { get; } = EntityUid;

}
