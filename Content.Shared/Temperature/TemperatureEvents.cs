using Content.Shared.Inventory;

namespace Content.Shared.Temperature;

[ByRefEvent]
public record struct BeforeHeatExchangeEvent(float Conductivity, bool Heating) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;
}

[ByRefEvent]
public record struct TemperatureChangedEvent(float CurrentTemperature, float LastTemperature)
{
    public readonly float CurrentTemperature = CurrentTemperature;
    public readonly float LastTemperature = LastTemperature;
}
