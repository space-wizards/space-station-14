using Content.Shared.Inventory;

namespace Content.Shared.Temperature;

public sealed class ModifyChangedTemperatureEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

    public float TemperatureDelta;

    public ModifyChangedTemperatureEvent(float temperature)
    {
        TemperatureDelta = temperature;
    }
}

public sealed class OnTemperatureChangeEvent : EntityEventArgs
{
    public float CurrentTemperature { get; }
    public float LastTemperature { get; }
    public float TemperatureDelta { get; }

    public OnTemperatureChangeEvent(float current, float last, float delta)
    {
        CurrentTemperature = current;
        LastTemperature = last;
        TemperatureDelta = delta;
    }
}

